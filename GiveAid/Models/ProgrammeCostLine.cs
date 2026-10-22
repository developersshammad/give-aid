using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiveAid.Models
{
    /// <summary>
    /// One budget / spending line of a programme, e.g. "Spectacles - Rs 180,000".
    /// The AI Programme Planner uses past lines to estimate the budget of a new programme.
    /// </summary>
    public class ProgrammeCostLine
    {
        public int Id { get; set; }

        public int ProgrammeId { get; set; }
        public Programme? Programme { get; set; }

        [Required, MaxLength(100)]
        public string Item { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
    }
}
