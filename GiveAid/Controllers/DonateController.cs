using System.Security.Cryptography;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>
    /// Donate: choose cause + amount, enter card details (dummy payment with full validation),
    /// then show a receipt. Works for guests and logged-in members.
    /// </summary>
    public class DonateController : Controller
    {
        private readonly GiveAidContext _db;
        public DonateController(GiveAidContext db) => _db = db;

        // GET: /Donate?causeId=2&amount=5000
        public async Task<IActionResult> Index(int? causeId, decimal? amount)
        {
            var vm = new DonateViewModel
            {
                CauseId = causeId,
                Amount = amount,
                Causes = await CauseStats.LoadAsync(_db, activeOnly: true)
            };
            // logged-in members do not need to type their name/e-mail again
            if (User.GetMemberId() != null)
            {
                vm.DonorName = User.GetDisplayName();
                vm.DonorEmail = User.GetEmail();
            }
            return View(vm);
        }

        // POST: /Donate
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DonateViewModel model)
        {
            // 1) cause must exist and be active
            var cause = model.CauseId.HasValue
                ? await _db.Causes.FirstOrDefaultAsync(c => c.Id == model.CauseId && c.IsActive)
                : null;
            if (model.CauseId.HasValue && cause == null)
                ModelState.AddModelError(nameof(model.CauseId), "This cause is not accepting donations.");

            // 2) card checks (brand, length, Luhn, expiry, CVV, name)
            foreach (var (field, error) in CardValidator.Validate(model.CardNumber, model.CardHolder, model.Expiry, model.Cvv))
                ModelState.AddModelError(field, error);

            if (!ModelState.IsValid || cause == null)
            {
                model.Causes = await CauseStats.LoadAsync(_db, activeOnly: true);
                model.Cvv = "";   // never send the CVV back to the browser
                return View(model);
            }

            // 3) "process" the payment - here we simply record a successful transaction
            string digits = CardValidator.Clean(model.CardNumber);
            var donation = new Donation
            {
                TransactionRef = NewTransactionRef(),
                CauseId = cause.Id,
                MemberId = User.GetMemberId(),
                DonorName = model.DonorName.Trim(),
                DonorEmail = model.DonorEmail.Trim().ToLowerInvariant(),
                Amount = Math.Round(model.Amount!.Value, 0),
                CardType = model.CardType,
                CardBrand = CardValidator.DetectBrand(digits),
                CardHolder = model.CardHolder.Trim().ToUpperInvariant(),
                CardLast4 = digits[^4..],                // only last four digits are stored
                Message = model.Message?.Trim(),
                IsAnonymous = model.IsAnonymous,
                Status = "Successful"
            };
            _db.Donations.Add(donation);
            await _db.SaveChangesAsync();

            // Post/Redirect/Get: refreshing the receipt page will not donate twice
            return RedirectToAction(nameof(Receipt), new { id = donation.TransactionRef });
        }

        // GET: /Donate/Receipt/GA-20260929-4F7A21
        public async Task<IActionResult> Receipt(string id)
        {
            var donation = await _db.Donations.Include(d => d.Cause)
                .FirstOrDefaultAsync(d => d.TransactionRef == id);
            if (donation == null) return NotFound();
            return View(donation);
        }

        /// <summary>Unique, hard-to-guess reference: GA-yyyyMMdd-XXXXXX</summary>
        private static string NewTransactionRef() =>
            $"GA-{DateTime.Now:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";
    }
}
