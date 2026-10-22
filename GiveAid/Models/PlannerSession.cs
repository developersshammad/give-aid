using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>
    /// One run of the AI Programme Planner: the goal the admin typed, and its result.
    /// Every action the planner takes or proposes is stored as a <see cref="PlannerStep"/>,
    /// so there is a full audit trail of what the AI suggested and what the admin decided.
    /// </summary>
    public class PlannerSession
    {
        public int Id { get; set; }

        /// <summary>The goal in the admin's own words, e.g. "Eye camp in Mithi for 1,500 patients in November".</summary>
        [Required, MaxLength(1000)]
        public string Goal { get; set; } = "";

        /// <summary>Planning / AwaitingApproval / Completed / Cancelled / Failed</summary>
        [MaxLength(30)]
        public string Status { get; set; } = "Planning";

        /// <summary>Which brain made the plan: Demo (built-in rules), Claude or OpenAI.</summary>
        [MaxLength(20)]
        public string Mode { get; set; } = "Demo";

        /// <summary>The planner's closing summary (or the error message when it failed).</summary>
        [MaxLength(4000)]
        public string? Summary { get; set; }

        /// <summary>The draft programme created after the admin approved it.</summary>
        public int? ProgrammeId { get; set; }
        public Programme? Programme { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
