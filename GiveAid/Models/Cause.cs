using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiveAid.Models
{
    /// <summary>Donation category, e.g. Children, Education, Disabled, Women, Youth, Elderly.</summary>
    public class Cause
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = "";

        [Required, MaxLength(500)]
        public string Description { get; set; } = "";

        /// <summary>Bootstrap-Icons name shown on the website, e.g. "backpack".</summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "heart";

        /// <summary>Fund-raising target in rupees (used for the progress bar).</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Goal amount (Rs)")]
        [Range(0, 1000000000, ErrorMessage = "Enter a valid amount.")]
        public decimal GoalAmount { get; set; }

        [Display(Name = "Display order")]

        public int DisplayOrder { get; set; }

        /// <summary>Only active causes appear in the donation form.</summary>
        [Display(Name = "Active (show in donation form)")]
        public bool IsActive { get; set; } = true;
    }
}
