using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Associated NGO that works under the Give-AID umbrella.</summary>
    public class Ngo
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = "";

        [MaxLength(50)]
        [Display(Name = "Registration no.")]
        public string? RegistrationNo { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Focus area")]
        public string FocusArea { get; set; } = "";

        [MaxLength(60)]
        public string? City { get; set; }

        [Display(Name = "Established (year)")]
        [Range(1900, 2100, ErrorMessage = "Enter a valid year.")]

        public int? EstablishedYear { get; set; }

        [MaxLength(100)]
        [Display(Name = "Contact person")]
        public string? ContactPerson { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [Required, MaxLength(1500)]
        public string Description { get; set; } = "";

        [MaxLength(250)]
        public string? LogoPath { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
