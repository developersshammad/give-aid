using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Registered website user (USER module). Holds personal + professional data.</summary>
    public class Member
    {
        public int Id { get; set; }

        // ----- personal data -----
        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required, MaxLength(150)]
        public string Email { get; set; } = "";

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = "";

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(60)]
        public string? City { get; set; }

        [MaxLength(60)]
        public string? Country { get; set; }

        // ----- professional data -----
        [MaxLength(100)]
        public string? Occupation { get; set; }

        [MaxLength(150)]
        public string? Organization { get; set; }

        [MaxLength(100)]
        public string? Qualification { get; set; }

        [MaxLength(300)]
        public string? AreasOfInterest { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        /// <summary>Admin can block a member by setting this to false.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>True once the member has typed the code sent to their e-mail (members created before this feature count as verified).</summary>
        [Display(Name = "E-mail verified")]
        public bool EmailVerified { get; set; } = true;
    }
}
