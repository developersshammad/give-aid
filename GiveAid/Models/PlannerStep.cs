using System.ComponentModel.DataAnnotations;

namespace GiveAid.Models
{
    /// <summary>
    /// One action of the AI Programme Planner.
    /// "Read" tools (find NGO, check calendar, weather ...) run straight away and are stored as Done.
    /// "Write" tools (create draft programme, send invitations) are only PROPOSED: they wait as Pending
    /// until an admin approves, edits or rejects them.
    /// </summary>
    public class PlannerStep
    {
        public int Id { get; set; }

        public int SessionId { get; set; }
        public PlannerSession? Session { get; set; }

        /// <summary>Position in the plan (1, 2, 3 ...).</summary>
        public int Order { get; set; }

        /// <summary>Tool name, e.g. find_ngos, propose_draft_programme.</summary>
        [Required, MaxLength(50)]
        public string Tool { get; set; } = "";

        [Required, MaxLength(150)]
        public string Title { get; set; } = "";

        /// <summary>Why the planner did / proposes this, in plain language.</summary>
        [MaxLength(2000)]
        public string? Explanation { get; set; }

        /// <summary>Tool input as JSON (for write steps: the proposal the admin can edit).</summary>
        public string InputJson { get; set; } = "{}";

        /// <summary>Tool output as JSON.</summary>
        public string? ResultJson { get; set; }

        /// <summary>Which data the step used, e.g. "Associated NGOs table".</summary>
        [MaxLength(300)]
        public string? Sources { get; set; }

        /// <summary>True for write tools: nothing happens until an admin approves.</summary>
        public bool RequiresApproval { get; set; }

        /// <summary>Done / Pending / Approved / Rejected / Failed / NeedsReview</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Done";

        [MaxLength(100)]
        public string? DecidedBy { get; set; }
        public DateTime? DecidedAt { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
