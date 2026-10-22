using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Gallery: admin uploads photos of the programmes conducted.</summary>
    public class GalleryController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        private readonly FileStorage _files;

        public GalleryController(GiveAidContext db, FileStorage files)
        {
            _db = db;
            _files = files;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Programmes = new SelectList(await _db.Programmes.OrderByDescending(p => p.StartDate).ToListAsync(), "Id", "Title");
            return View(await _db.GalleryImages.Include(g => g.Programme).OrderByDescending(g => g.UploadedAt).ToListAsync());
        }

        // POST: /Admin/Gallery/Upload
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string title, string? caption, int? programmeId, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
            {
                Error("Enter a title (maximum 150 characters).");
                return RedirectToAction(nameof(Index));
            }
            var fileError = FileStorage.Check(image, required: true);
            if (fileError != null)
            {
                Error(fileError);
                return RedirectToAction(nameof(Index));
            }

            _db.GalleryImages.Add(new GalleryImage
            {
                Title = title.Trim(),
                Caption = caption?.Trim() is { Length: > 0 } c ? (c.Length > 300 ? c[..300] : c) : null,
                ProgrammeId = programmeId,
                ImagePath = await _files.SaveAsync(image!, "gallery")
            });
            await _db.SaveChangesAsync();
            Success("Photo added to the gallery.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var image = await _db.GalleryImages.FindAsync(id);
            if (image == null) return NotFound();
            _files.Delete(image.ImagePath);
            _db.GalleryImages.Remove(image);
            await _db.SaveChangesAsync();
            Success("Photo removed.");
            return RedirectToAction(nameof(Index));
        }
    }
}
