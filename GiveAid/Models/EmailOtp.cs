using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>
    /// A one-time code (OTP) sent by e-mail: to verify a new member's e-mail address
    /// ("verify") or to reset a forgotten password ("reset").
    /// Only a salted hash of the code is stored, never the code itself.
    /// </summary>
    public class EmailOtp
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; } = "";

        /// <summary>"verify" or "reset".</summary>
        [Required, MaxLength(20)]
        public string Purpose { get; set; } = "verify";

        /// <summary>salt:hash (Base64) of the 6-digit code.</summary>
        [Required, MaxLength(100)]
        public string CodeHash { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        /// <summary>Wrong codes typed so far (the code stops working after 5).</summary>
        public int Attempts { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>When the code was used successfully (a code works only once).</summary>
        public DateTime? UsedAt { get; set; }
    }
}
