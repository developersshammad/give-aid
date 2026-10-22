using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Photo from a programme, uploaded by the admin.</summary>
    public class GalleryImage
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = "";

        [MaxLength(300)]
        public string? Caption { get; set; }

        [Required, MaxLength(250)]
        public string ImagePath { get; set; } = "";

        [Display(Name = "Programme")]

        public int? ProgrammeId { get; set; }
        public Programme? Programme { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
