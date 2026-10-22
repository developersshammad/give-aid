using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Donation causes (Children, Education, Disabled, Women, Youth, Elderly ...): add / edit / delete.</summary>
    public class CausesController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public CausesController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index() => View(await CauseStats.LoadAsync(_db, activeOnly: false));

        public IActionResult Create() =>
            View("Form", new Cause { Icon = "heart", GoalAmount = 500000, DisplayOrder = 10 });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Icon,GoalAmount,DisplayOrder,IsActive")] Cause cause)
        {
            if (await _db.Causes.AnyAsync(c => c.Name == cause.Name))
                ModelState.AddModelError(nameof(cause.Name), "A cause with this name already exists.");
            if (!ModelState.IsValid) return View("Form", cause);

            _db.Causes.Add(cause);
            await _db.SaveChangesAsync();
            Success($"Cause \"{cause.Name}\" added.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cause = await _db.Causes.FindAsync(id);
            return cause == null ? NotFound() : View("Form", cause);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Description,Icon,GoalAmount,DisplayOrder,IsActive")] Cause input)
        {
            var cause = await _db.Causes.FindAsync(id);
            if (cause == null) return NotFound();
            if (await _db.Causes.AnyAsync(c => c.Name == input.Name && c.Id != id))
                ModelState.AddModelError(nameof(input.Name), "A cause with this name already exists.");
            if (!ModelState.IsValid) { input.Id = id; return View("Form", input); }

            cause.Name = input.Name;
            cause.Description = input.Description;
            cause.Icon = string.IsNullOrWhiteSpace(input.Icon) ? "heart" : input.Icon.Trim();
            cause.GoalAmount = input.GoalAmount;
            cause.DisplayOrder = input.DisplayOrder;
            cause.IsActive = input.IsActive;
            await _db.SaveChangesAsync();
            Success($"Cause \"{cause.Name}\" updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cause = await _db.Causes.FindAsync(id);
            if (cause == null) return NotFound();

            // causes with donations are kept for the payment history - hide them instead
            if (await _db.Donations.AnyAsync(d => d.CauseId == id))
            {
                cause.IsActive = false;
                await _db.SaveChangesAsync();
                Error($"\"{cause.Name}\" already has donations, so it was deactivated instead of deleted.");
            }
            else
            {
                _db.Causes.Remove(cause);
                await _db.SaveChangesAsync();
                Success($"Cause \"{cause.Name}\" deleted.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
