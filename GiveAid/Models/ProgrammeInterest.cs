using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>A user's request to take part in an upcoming programme.</summary>
    public class ProgrammeInterest
    {
        public int Id { get; set; }

        public int ProgrammeId { get; set; }
        public Programme? Programme { get; set; }

        public int? MemberId { get; set; }
        public Member? Member { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Required, MaxLength(150)]
        public string Email { get; set; } = "";

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(40)]
        public string ParticipationType { get; set; } = "Volunteer";   // Volunteer / Participant / Sponsor

        [MaxLength(500)]
        public string? Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}
