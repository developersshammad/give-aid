using System.Diagnostics;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>Home page, Contact Us page and error pages.</summary>
    public class HomeController : Controller
    {
        private readonly GiveAidContext _db;
        public HomeController(GiveAidContext db) => _db = db;

        // GET: /
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var vm = new HomeViewModel
            {
                Intro = await _db.ContentPages.FirstOrDefaultAsync(p => p.Slug == "home"),
                Causes = await CauseStats.LoadAsync(_db, activeOnly: true),
                Upcoming = await _db.Programmes.Include(p => p.Ngo)
                    .Where(p => p.IsPublished && (p.EndDate ?? p.StartDate) >= today)
                    .OrderBy(p => p.StartDate).Take(3).ToListAsync(),
                Partners = await _db.Partners.OrderBy(p => p.DisplayOrder).ToListAsync(),
                Gallery = await _db.GalleryImages.OrderByDescending(g => g.UploadedAt).Take(8).ToListAsync(),
                RecentDonations = await _db.Donations.Include(d => d.Cause)
                    .OrderByDescending(d => d.DonatedAt).Take(6).ToListAsync(),
                TotalRaised = await _db.Donations.SumAsync(d => (decimal?)d.Amount) ?? 0,
                DonorCount = await _db.Donations.Select(d => d.DonorEmail).Distinct().CountAsync(),
                NgoCount = await _db.Ngos.CountAsync(),
                ProgrammeCount = await _db.Programmes.CountAsync(p => p.IsPublished)
            };
            return View(vm);
        }

        // GET: /Home/Contact
        public async Task<IActionResult> Contact()
        {
            var vm = new ContactViewModel { Page = await PageOrEmpty("contact-us") };
            // pre-fill name & e-mail for logged-in members
            if (User.GetMemberId() != null)
            {
                vm.Form.Name = User.GetDisplayName();
                vm.Form.Email = User.GetEmail();
            }
            return View(vm);
        }

        // POST: /Home/Contact  - message is stored as a query for the admin to answer
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact([Bind(Prefix = "Form")] QueryViewModel form)
        {
            if (!ModelState.IsValid)
                return View(new ContactViewModel { Page = await PageOrEmpty("contact-us"), Form = form });

            _db.Queries.Add(new SupportQuery
            {
                MemberId = User.GetMemberId(),
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                Subject = form.Subject.Trim(),
                Message = form.Message.Trim()
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Thank you! Your message has been sent. We will reply by e-mail.";
            return RedirectToAction(nameof(Contact));
        }

        private async Task<ContentPage> PageOrEmpty(string slug) =>
            await _db.ContentPages.FirstOrDefaultAsync(p => p.Slug == slug)
            ?? new ContentPage { Title = "Contact us", Body = "" };

        public IActionResult NotFoundPage() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
