using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiveAid.Models
{
    /// <summary>
    /// One (dummy) payment transaction. For security the full card number and CVV
    /// are NEVER saved - only the card brand and the last four digits.
    /// </summary>
    public class Donation
    {
        public int Id { get; set; }

        /// <summary>Unique reference shown on the receipt, e.g. GA-20260929-4F7A21.</summary>
        [Required, MaxLength(30)]
        public string TransactionRef { get; set; } = "";

        public int CauseId { get; set; }
        public Cause? Cause { get; set; }

        /// <summary>Null when the donor was not logged in (guest donation).</summary>
        public int? MemberId { get; set; }
        public Member? Member { get; set; }

        [Required, MaxLength(100)]
        public string DonorName { get; set; } = "";

        [Required, MaxLength(150)]
        public string DonorEmail { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string CardType { get; set; } = "Credit";   // Credit / Debit

        [MaxLength(20)]
        public string CardBrand { get; set; } = "";         // Visa / MasterCard / ...

        [MaxLength(100)]
        public string CardHolder { get; set; } = "";

        [MaxLength(4)]
        public string CardLast4 { get; set; } = "";

        [MaxLength(20)]
        public string Status { get; set; } = "Successful";

        [MaxLength(300)]
        public string? Message { get; set; }

        /// <summary>Donor asked not to show their name publicly.</summary>
        public bool IsAnonymous { get; set; }

        public DateTime DonatedAt { get; set; } = DateTime.Now;
    }
}
