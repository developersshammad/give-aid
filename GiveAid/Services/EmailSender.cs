using System.Net;
using System.Net.Mail;

namespace GiveAid.Services
{
    public interface IEmailSender
    {
        /// <summary>True when an e-mail service (Brevo API key or SMTP host) is filled in.</summary>
        bool IsConfigured { get; }

        /// <summary>Sends an e-mail. Returns true on success, false if not configured or failed.</summary>
        Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    /// <summary>
    /// Sends the website's e-mails (verification codes, password reset, invitations, query replies).
    ///
    /// Two ways, chosen in appsettings.json:
    ///   "Brevo": { "ApiKey": "...", "FromEmail": "...", "FromName": "Give-AID" }
    ///       Sends through the Brevo e-mail API over HTTPS. Use this on hosting that blocks
    ///       SMTP (for example the MonsterASP.NET free plan). Free plan: 300 e-mails a day.
    ///   "Smtp": { "Host": "smtp.gmail.com", "Port": 587, ... }
    ///       Classic SMTP (Gmail with an app password, Outlook, your host's mail server).
    /// Brevo is used when its API key is set; otherwise SMTP; otherwise nothing is sent
    /// and the website falls back to "mailto" links where it can.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, IHttpClientFactory http, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _http = http;
            _logger = logger;
        }

        // a value still starting with "YOUR_" is the template placeholder, not a real setting
        private static bool Set(string? v) => !string.IsNullOrWhiteSpace(v) && !v.StartsWith("YOUR_", StringComparison.Ordinal);
        private bool UseBrevo => Set(_config["Brevo:ApiKey"]) && Set(_config["Brevo:FromEmail"]);
        private bool UseSmtp => Set(_config["Smtp:Host"]);

        public bool IsConfigured => UseBrevo || UseSmtp;

        public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (UseBrevo) return await SendBrevoAsync(toEmail, toName, subject, htmlBody);
            if (UseSmtp) return await SendSmtpAsync(toEmail, toName, subject, htmlBody);
            return false;
        }

        // ---------------- Brevo (HTTPS API) ----------------
        private async Task<bool> SendBrevoAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var b = _config.GetSection("Brevo");
            try
            {
                var client = _http.CreateClient("brevo");
                using var req = new HttpRequestMessage(HttpMethod.Post, (b["BaseUrl"] ?? "https://api.brevo.com").TrimEnd('/') + "/v3/smtp/email")
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sender = new { name = b["FromName"] ?? "Give-AID", email = b["FromEmail"] ?? "" },
                        to = new[] { new { email = toEmail, name = string.IsNullOrWhiteSpace(toName) ? toEmail : toName } },
                        subject,
                        htmlContent = htmlBody
                    }), System.Text.Encoding.UTF8, "application/json")
                };
                req.Headers.Add("api-key", b["ApiKey"]);
                req.Headers.Add("accept", "application/json");
                using var res = await client.SendAsync(req);
                if (res.IsSuccessStatusCode) return true;
                _logger.LogWarning("Brevo refused the e-mail to {Email}: {Status} {Body}", toEmail, (int)res.StatusCode, await res.Content.ReadAsStringAsync());
                return false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "E-mail to {Email} could not be sent through Brevo", toEmail);
                return false;
            }
        }

        // ---------------- SMTP ----------------
        private async Task<bool> SendSmtpAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                var s = _config.GetSection("Smtp");
                using var client = new SmtpClient(s["Host"], int.TryParse(s["Port"], out var p) ? p : 587)
                {
                    EnableSsl = bool.TryParse(s["EnableSsl"], out var ssl) && ssl,
                    Credentials = new NetworkCredential(s["UserName"], s["Password"])
                };
                using var mail = new MailMessage
                {
                    From = new MailAddress(s["FromEmail"] ?? "no-reply@giveaid.org", s["FromName"] ?? "Give-AID"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(toEmail, toName));
                await client.SendMailAsync(mail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "E-mail to {Email} could not be sent", toEmail);
                return false;
            }
        }
    }
}
