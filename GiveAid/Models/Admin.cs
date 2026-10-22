using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>Administrator account that manages all website content (ADMIN module).</summary>
    public class Admin
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required, MaxLength(150)]
        public string Email { get; set; } = "";

        /// <summary>PBKDF2 hash - plain passwords are never stored.</summary>
        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
