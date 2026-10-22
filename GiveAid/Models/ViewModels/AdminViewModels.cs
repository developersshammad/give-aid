using System.ComponentModel.DataAnnotations;
using GiveAid.Models;

namespace GiveAid.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRaised { get; set; }
        public decimal RaisedThisMonth { get; set; }
        public int DonationCount { get; set; }
        public int MemberCount { get; set; }
        public int NgoCount { get; set; }
        public int UpcomingProgrammes { get; set; }
        public int OpenQueries { get; set; }
        public int InterestCount { get; set; }
        public List<Donation> RecentDonations { get; set; } = new();
        public List<SupportQuery> LatestQueries { get; set; } = new();
        public List<CauseProgress> Causes { get; set; } = new();

        // ----- extra data for the dashboard design -----
        public string AdminName { get; set; } = "";
        public decimal AverageGift => DonationCount == 0 ? 0 : TotalRaised / DonationCount;
        public int NewMembersThisMonth { get; set; }
        public int PartnerCount { get; set; }
        public List<WeekTotal> Weeks { get; set; } = new();
        public List<Programme> NextProgrammes { get; set; } = new();
        public Dictionary<int, int> InterestsPerProgramme { get; set; } = new();
        public List<Member> NewestMembers { get; set; } = new();
        public DateTime? OldestOpenQuery { get; set; }
    }

    /// <summary>Donations in one week (Monday to Sunday) for the dashboard chart.</summary>
    public class WeekTotal
    {
        public DateTime Start { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
        public bool IsCurrent { get; set; }
    }

    // ----- AI Programme Planner -----

    public class PlannerIndexViewModel
    {
        public List<PlannerSession> Sessions { get; set; } = new();
        public string Mode { get; set; } = "Demo";
        public string? ModeNote { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Describe the programme you want to plan.")]
        [System.ComponentModel.DataAnnotations.StringLength(1000, MinimumLength = 10, ErrorMessage = "Write 10 to 1000 characters.")]
        public string Goal { get; set; } = "";
    }

    public class PlannerDetailsViewModel
    {
        public PlannerSession Session { get; set; } = new();
        public List<PlannerStep> Steps { get; set; } = new();
        public Programme? Programme { get; set; }
        public Dictionary<int, string> NgoNames { get; set; } = new();
        /// <summary>Volunteer key -> (name, city, why) for the invitation step.</summary>
        public Dictionary<string, (string Name, string? City, string Why)> Volunteers { get; set; } = new();
    }

    /// <summary>Form for editing the proposed draft before approving it.</summary>
    public class DraftEditViewModel
    {
        public int StepId { get; set; }
        public int SessionId { get; set; }

        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150, MinimumLength = 3)]
        public string Title { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required]
        public string Category { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Display(Name = "Run by NGO")]
        public int? NgoId { get; set; }
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)]
        public string Venue { get; set; } = "";
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Start date")]
        public DateTime StartDate { get; set; }
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [System.ComponentModel.DataAnnotations.Display(Name = "End date")]
        public DateTime? EndDate { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, 10000000)]
        [System.ComponentModel.DataAnnotations.Display(Name = "People served")]
        public int? Beneficiaries { get; set; }
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(300)]
        public string Summary { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(4000)]
        public string Details { get; set; } = "";
        public List<GiveAid.Services.Planner.BudgetLine> Lines { get; set; } = new();
    }

    public class ReplyViewModel
    {
        public SupportQuery? Query { get; set; }

        public int Id { get; set; }

        [Required(ErrorMessage = "Write a reply.")]
        [StringLength(2000, MinimumLength = 2)]
        public string Reply { get; set; } = "";
    }

    public class DonationReportViewModel
    {
        public List<Donation> Donations { get; set; } = new();
        public List<Cause> Causes { get; set; } = new();
        public int? CauseId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public decimal Total => Donations.Sum(d => d.Amount);
    }

    public class MemberDetailsViewModel
    {
        public Member Member { get; set; } = new();
        public List<Donation> Donations { get; set; } = new();
        public List<Invitation> Invitations { get; set; } = new();
        public List<ProgrammeInterest> Interests { get; set; } = new();
    }
}
