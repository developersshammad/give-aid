using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>"Our Programmes" - list, details and the "I want to take part" form.</summary>
    public class ProgrammesController : Controller
    {
        private readonly GiveAidContext _db;
        public ProgrammesController(GiveAidContext db) => _db = db;

        // GET: /Programmes?category=Education
        public async Task<IActionResult> Index(string? category)
        {
            var today = DateTime.Today;
            // drafts made by the AI Programme Planner stay hidden until an admin publishes them
            var query = _db.Programmes.Include(p => p.Ngo).Where(p => p.IsPublished);
            if (!string.IsNullOrEmpty(category)) query = query.Where(p => p.Category == category);

            var all = await query.ToListAsync();
            var vm = new ProgrammeListViewModel
            {
                Category = category,
                Categories = await _db.Programmes.Where(p => p.IsPublished).Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync(),
                Upcoming = all.Where(p => p.IsUpcoming).OrderBy(p => p.StartDate).ToList(),
                Past = all.Where(p => !p.IsUpcoming).OrderByDescending(p => p.StartDate).ToList()
            };
            return View(vm);
        }

        // GET: /Programmes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await BuildDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: /Programmes/Interest  - user sends interest to take part
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Interest([Bind(Prefix = "Form")] InterestViewModel form)
        {
            var programme = await _db.Programmes.FindAsync(form.ProgrammeId);
            if (programme == null || !programme.IsPublished) return NotFound();
            if (!programme.IsUpcoming)
                ModelState.AddModelError("", "This programme has already finished.");

            var email = (form.Email ?? "").Trim().ToLowerInvariant();
            if (await _db.ProgrammeInterests.AnyAsync(i => i.ProgrammeId == programme.Id && i.Email == email))
                ModelState.AddModelError("Form.Email", "This e-mail is already registered for this programme.");

            if (!ModelState.IsValid)
            {
                var vm = (await BuildDetailsAsync(programme.Id))!;
                vm.Form = form;
                return View(nameof(Details), vm);
            }

            _db.ProgrammeInterests.Add(new ProgrammeInterest
            {
                ProgrammeId = programme.Id,
                MemberId = User.GetMemberId(),
                Name = form.Name.Trim(),
                Email = email,
                Phone = form.Phone?.Trim(),
                ParticipationType = form.ParticipationType,
                Message = form.Message?.Trim()
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Thank you, {form.Name}! The organisers of \"{programme.Title}\" will contact you soon.";
            return RedirectToAction(nameof(Details), new { id = programme.Id });
        }

        private async Task<ProgrammeDetailsViewModel?> BuildDetailsAsync(int id)
        {
            var programme = await _db.Programmes.Include(p => p.Ngo).FirstOrDefaultAsync(p => p.Id == id);
            if (programme == null) return null;

            // A draft is only visible to a logged-in admin (the "Preview" button in the AI Planner).
            if (!programme.IsPublished)
            {
                var admin = await HttpContext.AuthenticateAsync(AuthSchemes.Admin);
                if (!admin.Succeeded || admin.Principal?.IsInRole("Admin") != true) return null;
                ViewData["DraftPreview"] = true;
            }

            var vm = new ProgrammeDetailsViewModel
            {
                Programme = programme,
                Photos = await _db.GalleryImages.Where(g => g.ProgrammeId == id).ToListAsync(),
                InterestCount = await _db.ProgrammeInterests.CountAsync(i => i.ProgrammeId == id),
                Form = new InterestViewModel { ProgrammeId = id }
            };

            var memberId = User.GetMemberId();
            if (memberId != null)
            {
                vm.Form.Name = User.GetDisplayName();
                vm.Form.Email = User.GetEmail();
                vm.AlreadyJoined = await _db.ProgrammeInterests.AnyAsync(i => i.ProgrammeId == id && i.MemberId == memberId);
            }
            return vm;
        }
    }
}
