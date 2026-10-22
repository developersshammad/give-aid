using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Company associated with Give-AID, listed with its logo on "Our Partners".</summary>
    public class Partner
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [MaxLength(40)]
        [Display(Name = "Partner type")]
        public string PartnerType { get; set; } = "Corporate";   // Corporate / Media / Technology / Foundation

        [MaxLength(400)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(250)]
        [Display(Name = "Logo")]
        public string? LogoPath { get; set; }

        [Display(Name = "Display order")]

        public int DisplayOrder { get; set; }
    }
}
