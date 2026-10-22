using System.ComponentModel.DataAnnotations;
using GiveAid.Models;

namespace GiveAid.Models.ViewModels
{
    /// <summary>Everything the home page shows.</summary>
    public class HomeViewModel
    {
        public ContentPage? Intro { get; set; }
        public List<CauseProgress> Causes { get; set; } = new();
        public List<Programme> Upcoming { get; set; } = new();
        public List<Partner> Partners { get; set; } = new();
        public List<GalleryImage> Gallery { get; set; } = new();
        public List<Donation> RecentDonations { get; set; } = new();
        public decimal TotalRaised { get; set; }
        public int DonorCount { get; set; }
        public int NgoCount { get; set; }
        public int ProgrammeCount { get; set; }
    }

    /// <summary>About Us / Help / Contact page with its sibling links.</summary>
    public class PageViewModel
    {
        public ContentPage Page { get; set; } = new();
        public List<ContentPage> Siblings { get; set; } = new();
    }

    /// <summary>Query form used in the Help Centre and Contact Us pages.</summary>
    public class QueryViewModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Enter your e-mail so we can reply.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [StringLength(150)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter a subject.")]
        [StringLength(150)]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Write your question.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Please write at least 10 characters.")]
        public string Message { get; set; } = "";
    }

    public class HelpViewModel
    {
        public ContentPage Page { get; set; } = new();
        public QueryViewModel Form { get; set; } = new();
        public List<SupportQuery> MyQueries { get; set; } = new();
    }

    public class ContactViewModel
    {
        public ContentPage Page { get; set; } = new();
        public QueryViewModel Form { get; set; } = new();
    }

    /// <summary>"I want to take part" form on a programme page.</summary>
    public class InterestViewModel
    {
        public int ProgrammeId { get; set; }

        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Enter your e-mail.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [StringLength(150)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [RegularExpression(@"^[0-9+\- ]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
        public string? Phone { get; set; }

        [Required]
        [RegularExpression("^(Volunteer|Participant|Sponsor)$", ErrorMessage = "Choose how you want to take part.")]
        [Display(Name = "I want to join as")]
        public string ParticipationType { get; set; } = "Volunteer";

        [StringLength(500)]
        public string? Message { get; set; }
    }

    public class ProgrammeDetailsViewModel
    {
        public Programme Programme { get; set; } = new();
        public InterestViewModel Form { get; set; } = new();
        public List<GalleryImage> Photos { get; set; } = new();
        public int InterestCount { get; set; }
        public bool AlreadyJoined { get; set; }
    }

    public class ProgrammeListViewModel
    {
        public List<Programme> Upcoming { get; set; } = new();
        public List<Programme> Past { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? Category { get; set; }
    }

    /// <summary>Invite-a-friend form.</summary>
    public class InviteViewModel
    {
        [Required(ErrorMessage = "Enter your friend's name.")]
        [StringLength(100)]
        [Display(Name = "Friend's name")]
        public string FriendName { get; set; } = "";

        [Required(ErrorMessage = "Enter your friend's e-mail.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [StringLength(150)]
        [Display(Name = "Friend's e-mail")]
        public string FriendEmail { get; set; } = "";

        [StringLength(1000)]
        [Display(Name = "Personal message")]
        public string? PersonalMessage { get; set; }

        public List<Invitation> Sent { get; set; } = new();
    }
}
