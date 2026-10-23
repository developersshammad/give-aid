using System.Net;
using System.Security.Cryptography;
using System.Text;
using GiveAid.Data;
using GiveAid.Models;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Services
{
    /// <summary>Settings from appsettings.json → "EmailVerification".</summary>
    public class OtpOptions
    {
        /// <summary>When false, new members are verified straight away (no code). Codes are only required once an e-mail service is set up.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Minutes a code stays valid.</summary>
        public int CodeMinutes { get; set; } = 10;
        /// <summary>Wrong tries allowed per code.</summary>
        public int MaxAttempts { get; set; } = 5;
        /// <summary>Seconds to wait before asking for another code.</summary>
        public int ResendSeconds { get; set; } = 60;
        /// <summary>Codes one e-mail address can receive per hour.</summary>
        public int MaxPerHour { get; set; } = 5;
    }

    public enum OtpCheck { Ok, Wrong, Expired, TooManyTries, NoCode }

    public record OtpSendResult(bool Created, bool Emailed, string? Error);

    /// <summary>
    /// One-time codes for e-mail verification and password reset.
    ///  1. A random 6-digit code is made with a cryptographic random number generator.
    ///  2. Only a salted SHA-256 hash is saved, with an expiry time (10 minutes).
    ///  3. Checking compares hashes in constant time; 5 wrong tries end the code.
    ///  4. A new code needs a 60-second wait and at most 5 codes per hour per e-mail.
    /// </summary>
    public class OtpService
    {
        private readonly GiveAidContext _db;
        private readonly IEmailSender _email;
        private readonly OtpOptions _o;

        public OtpService(GiveAidContext db, IEmailSender email, Microsoft.Extensions.Options.IOptions<OtpOptions> options)
        {
            _db = db;
            _email = email;
            _o = options.Value;
        }

        public OtpOptions Options => _o;
        public bool EmailWorks => _email.IsConfigured;

        /// <summary>Seconds left before another code may be sent (0 = now).</summary>
        public async Task<int> WaitSecondsAsync(string email, string purpose)
        {
            var last = await _db.EmailOtps.Where(o => o.Email == email && o.Purpose == purpose)
                                          .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (last == null) return 0;
            var left = _o.ResendSeconds - (int)(DateTime.Now - last.CreatedAt).TotalSeconds;
            return Math.Max(0, left);
        }

        /// <summary>Makes a new code (older unused codes of the same purpose stop working) and e-mails it.</summary>
        public async Task<OtpSendResult> SendAsync(string email, string name, string purpose)
        {
            email = email.Trim().ToLowerInvariant();
            int wait = await WaitSecondsAsync(email, purpose);
            if (wait > 0) return new(false, false, $"A code was sent less than a minute ago — use that code, or ask for a new one in {wait} seconds.");
            var hourAgo = DateTime.Now.AddHours(-1);
            if (await _db.EmailOtps.CountAsync(o => o.Email == email && o.Purpose == purpose && o.CreatedAt > hourAgo) >= _o.MaxPerHour)
                return new(false, false, "Too many codes were requested. Please try again in an hour.");

            foreach (var old in await _db.EmailOtps.Where(o => o.Email == email && o.Purpose == purpose && o.UsedAt == null).ToListAsync())
                old.UsedAt = DateTime.Now;                                   // only the newest code works

            string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _db.EmailOtps.Add(new EmailOtp
            {
                Email = email, Purpose = purpose, CodeHash = Hash(code),
                ExpiresAt = DateTime.Now.AddMinutes(_o.CodeMinutes), CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            bool reset = purpose == "reset";
            string subject = reset ? $"{code} is your Give-AID password reset code" : $"{code} is your Give-AID verification code";
            string html = $@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #DCE7E1;border-radius:12px"">
<h2 style=""color:#0F4535;margin:0 0 12px"">Give-AID</h2>
<p>Dear {WebUtility.HtmlEncode(name)},</p>
<p>{(reset ? "Use this code to set a new password:" : "Use this code to verify your e-mail address:")}</p>
<p style=""font-size:32px;letter-spacing:8px;font-weight:700;color:#0F4535;background:#F4F9F6;padding:12px 16px;border-radius:8px;text-align:center"">{code}</p>
<p style=""color:#56665F"">The code works for {_o.CodeMinutes} minutes. If you did not ask for it, you can ignore this e-mail.</p></div>";
            bool sent = await _email.SendAsync(email, name, subject, html);
            return new(true, sent, sent ? null : "The code could not be e-mailed right now. Please try again in a minute.");
        }

        /// <summary>Checks a typed code. A correct code is used up straight away.</summary>
        public async Task<(OtpCheck Result, int TriesLeft)> CheckAsync(string email, string purpose, string? typed)
        {
            email = email.Trim().ToLowerInvariant();
            var otp = await _db.EmailOtps.Where(o => o.Email == email && o.Purpose == purpose && o.UsedAt == null)
                                         .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (otp == null) return (OtpCheck.NoCode, 0);
            if (otp.ExpiresAt < DateTime.Now) return (OtpCheck.Expired, 0);
            if (otp.Attempts >= _o.MaxAttempts) return (OtpCheck.TooManyTries, 0);

            string digits = new string((typed ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length == 6 && Matches(otp.CodeHash, digits))
            {
                otp.UsedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return (OtpCheck.Ok, 0);
            }
            otp.Attempts++;
            await _db.SaveChangesAsync();
            int left = _o.MaxAttempts - otp.Attempts;
            return (left <= 0 ? OtpCheck.TooManyTries : OtpCheck.Wrong, Math.Max(0, left));
        }

        public static string Message(OtpCheck r, int left) => r switch
        {
            OtpCheck.Wrong => $"That code is not correct. {left} {(left == 1 ? "try" : "tries")} left.",
            OtpCheck.Expired => "This code has expired. Ask for a new code.",
            OtpCheck.TooManyTries => "Too many wrong codes. Ask for a new code.",
            OtpCheck.NoCode => "There is no active code. Ask for a new code.",
            _ => ""
        };

        // ---- salted SHA-256: "salt:hash" ----
        private static string Hash(string code)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(Digest(salt, code));
        }

        private static bool Matches(string stored, string code)
        {
            var parts = stored.Split(':');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            return CryptographicOperations.FixedTimeEquals(Digest(salt, code), Convert.FromBase64String(parts[1]));
        }

        private static byte[] Digest(byte[] salt, string code) =>
            SHA256.HashData(salt.Concat(Encoding.UTF8.GetBytes(code)).ToArray());
    }
}
