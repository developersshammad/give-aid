using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Programme / event conducted by an NGO (Education, Health Care, Specially Challenged Children...).</summary>
    public class Programme
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = "";

        [Required, MaxLength(40)]
        public string Category { get; set; } = "Education";

        /// <summary>The NGO that runs this programme (optional).</summary>
        [Display(Name = "Run by NGO")]
        public int? NgoId { get; set; }
        public Ngo? Ngo { get; set; }

        [Required, MaxLength(150)]
        public string Venue { get; set; } = "";

        [Display(Name = "Start date"), DataType(DataType.Date)]

        public DateTime StartDate { get; set; }
        [Display(Name = "End date"), DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required, MaxLength(300)]
        public string Summary { get; set; } = "";

        [Required, MaxLength(4000)]
        public string Details { get; set; } = "";

        [MaxLength(250)]
        public string? ImagePath { get; set; }

        /// <summary>How many people the programme serves (used for budget estimates).</summary>
        [Display(Name = "People served")]
        [Range(0, 10000000, ErrorMessage = "Enter a valid number.")]
        public int? Beneficiaries { get; set; }

        /// <summary>
        /// False = draft (only admins can see it). Drafts are created by the AI Programme Planner
        /// and stay hidden from the public website until an admin publishes them.
        /// </summary>
        [Display(Name = "Published on the website")]
        public bool IsPublished { get; set; } = true;

        /// <summary>A programme is "upcoming" until its last day has passed (not stored in DB).</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsUpcoming => (EndDate ?? StartDate).Date >= DateTime.Today;
    }
}
