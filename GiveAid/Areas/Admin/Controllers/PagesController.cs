using GiveAid.Data;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>
    /// Edit the information part of the text pages: Home intro, About Us sub pages
    /// (What we do, Our mission, Our team, Career with us, Our achievements, Our supporters,
    /// Read about us), Help Centre and Contact Us.
    /// </summary>
    public class PagesController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public PagesController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.ContentPages.OrderBy(p => p.Section).ThenBy(p => p.DisplayOrder).ToListAsync());

        public async Task<IActionResult> Edit(int id)
        {
            var page = await _db.ContentPages.FindAsync(id);
            return page == null ? NotFound() : View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title,Subtitle,Body")] ContentPage input)
        {
            var page = await _db.ContentPages.FindAsync(id);
            if (page == null) return NotFound();

            // Slug and Section are fixed and are not posted by the form
            ModelState.Remove(nameof(ContentPage.Slug));
            ModelState.Remove(nameof(ContentPage.Section));
            if (!ModelState.IsValid)
            {
                input.Id = id; input.Slug = page.Slug; input.Section = page.Section;
                return View(input);
            }

            page.Title = input.Title.Trim();
            page.Subtitle = input.Subtitle?.Trim();
            page.Body = input.Body;
            page.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            Success($"Page \"{page.Title}\" saved.");
            return RedirectToAction(nameof(Index));
        }
    }
}
