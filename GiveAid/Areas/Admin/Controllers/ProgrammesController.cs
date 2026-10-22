using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Programmes conducted by the NGOs (Education, Health Care, Specially Challenged Children ...).</summary>
    public class ProgrammesController : AdminBaseController
    {
        public static readonly string[] Categories =
        {
            "Education", "Health Care", "Specially Challenged Children", "Children",
            "Women Empowerment", "Youth Development", "Elderly Care", "Disaster Relief"
        };
        private const string Fields = "Title,Category,NgoId,Venue,StartDate,EndDate,Summary,Details,Beneficiaries,IsPublished";

        private readonly GiveAidContext _db;
        private readonly FileStorage _files;

        public ProgrammesController(GiveAidContext db, FileStorage files)
        {
            _db = db;
            _files = files;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.InterestCounts = await _db.ProgrammeInterests.GroupBy(i => i.ProgrammeId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            return View(await _db.Programmes.Include(p => p.Ngo).OrderByDescending(p => p.StartDate).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await FillListsAsync();
            return View("Form", new Programme { StartDate = DateTime.Today.AddDays(14) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Fields)] Programme programme, IFormFile? image)
        {
            Validate(programme, image);
            if (!ModelState.IsValid) { await FillListsAsync(); return View("Form", programme); }

            if (image != null && image.Length > 0) programme.ImagePath = await _files.SaveAsync(image, "programmes");
            _db.Programmes.Add(programme);
            await _db.SaveChangesAsync();
            Success($"Programme \"{programme.Title}\" added.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var programme = await _db.Programmes.FindAsync(id);
            if (programme == null) return NotFound();
            await FillListsAsync();
            return View("Form", programme);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind(Fields)] Programme input, IFormFile? image)
        {
            var programme = await _db.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            Validate(input, image);
            if (!ModelState.IsValid)
            {
                input.Id = id; input.ImagePath = programme.ImagePath;
                await FillListsAsync();
                return View("Form", input);
            }

            programme.Title = input.Title;
            programme.Category = input.Category;
            programme.NgoId = input.NgoId;
            programme.Venue = input.Venue;
            programme.StartDate = input.StartDate;
            programme.EndDate = input.EndDate;
            programme.Summary = input.Summary;
            programme.Details = input.Details;
            programme.Beneficiaries = input.Beneficiaries;
            programme.IsPublished = input.IsPublished;
            if (image != null && image.Length > 0)
            {
                _files.Delete(programme.ImagePath);
                programme.ImagePath = await _files.SaveAsync(image, "programmes");
            }
            await _db.SaveChangesAsync();
            Success($"Programme \"{programme.Title}\" updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var programme = await _db.Programmes.FindAsync(id);
            if (programme == null) return NotFound();

            // gallery photos are kept but unlinked; interest requests are deleted (cascade)
            var photos = await _db.GalleryImages.Where(g => g.ProgrammeId == id).ToListAsync();
            foreach (var g in photos) g.ProgrammeId = null;
            var interests = await _db.ProgrammeInterests.Where(i => i.ProgrammeId == id).ToListAsync();
            _db.ProgrammeInterests.RemoveRange(interests);
            // planner runs that created this programme keep their history, but lose the link
            var sessions = await _db.PlannerSessions.Where(s => s.ProgrammeId == id).ToListAsync();
            foreach (var s in sessions) s.ProgrammeId = null;
            var costs = await _db.ProgrammeCostLines.Where(c => c.ProgrammeId == id).ToListAsync();
            _db.ProgrammeCostLines.RemoveRange(costs);

            _files.Delete(programme.ImagePath);
            _db.Programmes.Remove(programme);
            await _db.SaveChangesAsync();
            Success($"Programme \"{programme.Title}\" deleted.");
            return RedirectToAction(nameof(Index));
        }

        private void Validate(Programme p, IFormFile? image)
        {
            if (p.EndDate.HasValue && p.EndDate.Value.Date < p.StartDate.Date)
                ModelState.AddModelError(nameof(p.EndDate), "End date cannot be before the start date.");
            var fileError = FileStorage.Check(image, required: false);
            if (fileError != null) ModelState.AddModelError("image", fileError);
        }

        private async Task FillListsAsync()
        {
            ViewBag.Ngos = new SelectList(await _db.Ngos.OrderBy(n => n.Name).ToListAsync(), "Id", "Name");
            ViewBag.Categories = new SelectList(Categories);
        }
    }
}
