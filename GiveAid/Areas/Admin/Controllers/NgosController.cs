using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>List of other associated NGOs - admin adds / edits / deletes their details.</summary>
    public class NgosController : AdminBaseController
    {
        private const string Fields = "Name,RegistrationNo,FocusArea,City,EstablishedYear,ContactPerson,Email,Phone,Website,Description";
        private readonly GiveAidContext _db;
        private readonly FileStorage _files;

        public NgosController(GiveAidContext db, FileStorage files)
        {
            _db = db;
            _files = files;
        }

        public async Task<IActionResult> Index() => View(await _db.Ngos.OrderBy(n => n.Name).ToListAsync());

        public IActionResult Create() => View("Form", new Ngo());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Fields)] Ngo ngo, IFormFile? logo)
        {
            var fileError = FileStorage.Check(logo, required: false);
            if (fileError != null) ModelState.AddModelError("logo", fileError);
            if (!ModelState.IsValid) return View("Form", ngo);

            if (logo != null && logo.Length > 0) ngo.LogoPath = await _files.SaveAsync(logo, "ngos");
            _db.Ngos.Add(ngo);
            await _db.SaveChangesAsync();
            Success($"NGO \"{ngo.Name}\" added.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ngo = await _db.Ngos.FindAsync(id);
            return ngo == null ? NotFound() : View("Form", ngo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(Fields)] Ngo input, IFormFile? logo)
        {
            var ngo = await _db.Ngos.FindAsync(id);
            if (ngo == null) return NotFound();

            var fileError = FileStorage.Check(logo, required: false);
            if (fileError != null) ModelState.AddModelError("logo", fileError);
            if (!ModelState.IsValid) { input.Id = id; input.LogoPath = ngo.LogoPath; return View("Form", input); }

            ngo.Name = input.Name;
            ngo.RegistrationNo = input.RegistrationNo;
            ngo.FocusArea = input.FocusArea;
            ngo.City = input.City;
            ngo.EstablishedYear = input.EstablishedYear;
            ngo.ContactPerson = input.ContactPerson;
            ngo.Email = input.Email;
            ngo.Phone = input.Phone;
            ngo.Website = input.Website;
            ngo.Description = input.Description;
            if (logo != null && logo.Length > 0)
            {
                _files.Delete(ngo.LogoPath);
                ngo.LogoPath = await _files.SaveAsync(logo, "ngos");
            }
            await _db.SaveChangesAsync();
            Success($"NGO \"{ngo.Name}\" updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ngo = await _db.Ngos.FindAsync(id);
            if (ngo == null) return NotFound();

            // programmes of this NGO stay on the website, but without an NGO link
            var programmes = await _db.Programmes.Where(p => p.NgoId == id).ToListAsync();
            foreach (var p in programmes) p.NgoId = null;

            _files.Delete(ngo.LogoPath);
            _db.Ngos.Remove(ngo);
            await _db.SaveChangesAsync();
            Success($"NGO \"{ngo.Name}\" deleted.");
            return RedirectToAction(nameof(Index));
        }
    }
}
