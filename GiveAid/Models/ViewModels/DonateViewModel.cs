using System.ComponentModel.DataAnnotations;
using GiveAid.Models;

namespace GiveAid.Models.ViewModels
{
    /// <summary>
    /// Donation form. Card fields are checked by CardValidator (server) and donate.js (browser).
    /// Card number and CVV are used only for validation and are never saved.
    /// </summary>
    public class DonateViewModel
    {
        // ----- step 1: cause & amount -----
        [Required(ErrorMessage = "Choose the cause you want to support.")]
        [Display(Name = "Cause")]
        public int? CauseId { get; set; }

        [Required(ErrorMessage = "Enter the amount.")]
        [Range(100, 5000000, ErrorMessage = "Amount must be between Rs 100 and Rs 5,000,000.")]
        public decimal? Amount { get; set; }

        // ----- step 2: donor -----
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(100)]
        [Display(Name = "Your name")]
        public string DonorName { get; set; } = "";

        [Required(ErrorMessage = "Enter your e-mail for the receipt.")]
        [EmailAddress(ErrorMessage = "Enter a valid e-mail address.")]
        [StringLength(150)]
        [Display(Name = "E-mail")]
        public string DonorEmail { get; set; } = "";

        [StringLength(300)]
        public string? Message { get; set; }

        [Display(Name = "Don't show my name publicly")]
        public bool IsAnonymous { get; set; }

        // ----- step 3: card (dummy payment) -----
        [Required]
        [RegularExpression("^(Credit|Debit)$", ErrorMessage = "Choose credit or debit card.")]
        [Display(Name = "Card type")]
        public string CardType { get; set; } = "Credit";

        [Required(ErrorMessage = "Enter the card number.")]
        [Display(Name = "Card number")]
        public string CardNumber { get; set; } = "";

        [Required(ErrorMessage = "Enter the name on the card.")]
        [Display(Name = "Name on card")]
        public string CardHolder { get; set; } = "";

        [Required(ErrorMessage = "Enter the expiry date.")]
        [Display(Name = "Expiry (MM/YY)")]
        public string Expiry { get; set; } = "";

        [Required(ErrorMessage = "Enter the CVV.")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = "";

        // ----- display only (not posted) -----
        public List<CauseProgress> Causes { get; set; } = new();
    }

    /// <summary>A cause together with how much has been raised so far.</summary>
    public class CauseProgress
    {
        public Cause Cause { get; set; } = new();
        public decimal Raised { get; set; }
        public int Donors { get; set; }
        public int Percent => Cause.GoalAmount <= 0 ? 0 : (int)Math.Min(100, Math.Round(Raised * 100 / Cause.GoalAmount));
    }
}
