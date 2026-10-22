using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>
    /// Editable text page (About Us sub pages, Help Centre, Contact Us, Home intro).
    /// The admin edits Title / Subtitle / Body from the admin panel.
    /// Body format: blank line = new paragraph, "## " = sub-heading, "- " = bullet point.
    /// </summary>
    public class ContentPage
    {
        public int Id { get; set; }

        /// <summary>URL key, e.g. "our-mission".</summary>
        [Required, MaxLength(60)]
        public string Slug { get; set; } = "";

        /// <summary>Group: About / Help / Contact / Home.</summary>
        [Required, MaxLength(30)]
        public string Section { get; set; } = "About";

        [Required, MaxLength(120)]
        public string Title { get; set; } = "";

        [MaxLength(250)]
        public string? Subtitle { get; set; }

        [Required]
        public string Body { get; set; } = "";

        public int DisplayOrder { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
