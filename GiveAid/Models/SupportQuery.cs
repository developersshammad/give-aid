using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Question raised by a user (Help Centre / Contact Us). The admin replies to it.</summary>
    public class SupportQuery
    {
        public int Id { get; set; }

        public int? MemberId { get; set; }
        public Member? Member { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Required, MaxLength(150)]
        public string Email { get; set; } = "";

        [Required, MaxLength(150)]
        public string Subject { get; set; } = "";

        [Required, MaxLength(2000)]
        public string Message { get; set; } = "";

        [MaxLength(2000)]
        public string? Reply { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Open";   // Open / Answered

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? RepliedAt { get; set; }
    }
}
