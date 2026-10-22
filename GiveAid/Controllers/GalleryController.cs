using GiveAid.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>Photo gallery of programmes conducted (images are added by the admin).</summary>
    public class GalleryController : Controller
    {
        private readonly GiveAidContext _db;
        public GalleryController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.GalleryImages.Include(g => g.Programme)
                .OrderByDescending(g => g.UploadedAt).ToListAsync());
    }
}
