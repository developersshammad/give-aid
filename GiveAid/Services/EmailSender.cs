using System.Net;
using System.Net.Mail;

namespace GiveAid.Services
{
    public interface IEmailSender
    {
        /// <summary>True when SMTP settings are filled in appsettings.json.</summary>
        bool IsConfigured { get; }

        /// <summary>Sends an e-mail. Returns true on success, false if not configured or failed.</summary>
        Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody);
    }

    /// <summary>
    /// Sends mail through the SMTP server in appsettings.json ("Smtp" section).
    /// If no SMTP host is configured the website still works: invitations and
    /// replies are saved in the database and the user gets a "mailto" link instead.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Smtp:Host"]);

        public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (!IsConfigured) return false;
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
