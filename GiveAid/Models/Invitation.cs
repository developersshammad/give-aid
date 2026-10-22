using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Mail sent by a member to invite a friend to join the cause.</summary>
    public class Invitation
    {
        public int Id { get; set; }

        public int MemberId { get; set; }
        public Member? Member { get; set; }

        [Required, MaxLength(100)]
        public string FriendName { get; set; } = "";

        [Required, MaxLength(150)]
        public string FriendEmail { get; set; } = "";

        [MaxLength(1000)]
        public string? PersonalMessage { get; set; }

        /// <summary>True when the SMTP server accepted the mail.</summary>
        public bool EmailSent { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
