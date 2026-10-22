using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Partner companies with logo upload.</summary>
    public class PartnersController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        private readonly FileStorage _files;

        public PartnersController(GiveAidContext db, FileStorage files)
        {
            _db = db;
            _files = files;
        }

        public async Task<IActionResult> Index() =>
            View(await _db.Partners.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync());

        public IActionResult Create() => View("Form", new Partner { DisplayOrder = 10 });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PartnerType,Description,Website,DisplayOrder")] Partner partner, IFormFile? logo)
        {
            var fileError = FileStorage.Check(logo, required: true);
            if (fileError != null) ModelState.AddModelError("logo", fileError);
            if (!ModelState.IsValid) return View("Form", partner);

            partner.LogoPath = await _files.SaveAsync(logo!, "partners");
            _db.Partners.Add(partner);
            await _db.SaveChangesAsync();
            Success($"Partner \"{partner.Name}\" added.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var partner = await _db.Partners.FindAsync(id);
            return partner == null ? NotFound() : View("Form", partner);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,PartnerType,Description,Website,DisplayOrder")] Partner input, IFormFile? logo)
        {
            var partner = await _db.Partners.FindAsync(id);
            if (partner == null) return NotFound();

            var fileError = FileStorage.Check(logo, required: false);
            if (fileError != null) ModelState.AddModelError("logo", fileError);
            if (!ModelState.IsValid) { input.Id = id; input.LogoPath = partner.LogoPath; return View("Form", input); }

            partner.Name = input.Name;
            partner.PartnerType = input.PartnerType;
            partner.Description = input.Description;
            partner.Website = input.Website;
            partner.DisplayOrder = input.DisplayOrder;
            if (logo != null && logo.Length > 0)   // new logo replaces the old file
            {
                _files.Delete(partner.LogoPath);
                partner.LogoPath = await _files.SaveAsync(logo, "partners");
            }
            await _db.SaveChangesAsync();
            Success($"Partner \"{partner.Name}\" updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var partner = await _db.Partners.FindAsync(id);
            if (partner == null) return NotFound();
            _files.Delete(partner.LogoPath);
            _db.Partners.Remove(partner);
            await _db.SaveChangesAsync();
            Success($"Partner \"{partner.Name}\" deleted.");
            return RedirectToAction(nameof(Index));
        }
    }
}
