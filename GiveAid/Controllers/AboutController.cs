using GiveAid.Data;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>
    /// About Us section. Each sub link (What we do, Our mission, Our team, Career with us,
    /// Our achievements, Our supporters, Read about us) is a ContentPage edited by the admin.
    /// </summary>
    public class AboutController : Controller
    {
        private readonly GiveAidContext _db;
        public AboutController(GiveAidContext db) => _db = db;

        // GET: /About  or  /About/our-mission
        public async Task<IActionResult> Index(string? slug)
        {
            var pages = await _db.ContentPages.Where(p => p.Section == "About")
                .OrderBy(p => p.DisplayOrder).ToListAsync();
            if (pages.Count == 0) return NotFound();

            var page = string.IsNullOrEmpty(slug) ? pages[0] : pages.FirstOrDefault(p => p.Slug == slug);
            if (page == null) return NotFound();

            return View(new PageViewModel { Page = page, Siblings = pages });
        }
    }
}
