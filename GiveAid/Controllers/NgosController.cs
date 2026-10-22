using GiveAid.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>List of the NGOs associated with Give-AID.</summary>
    public class NgosController : Controller
    {
        private readonly GiveAidContext _db;
        public NgosController(GiveAidContext db) => _db = db;

        // GET: /Ngos?focus=Education
        public async Task<IActionResult> Index(string? focus)
        {
            ViewBag.FocusAreas = await _db.Ngos.Select(n => n.FocusArea).Distinct().OrderBy(f => f).ToListAsync();
            ViewBag.Focus = focus;

            var query = _db.Ngos.AsQueryable();
            if (!string.IsNullOrEmpty(focus)) query = query.Where(n => n.FocusArea == focus);
            return View(await query.OrderBy(n => n.Name).ToListAsync());
        }

        // GET: /Ngos/Details/3
        public async Task<IActionResult> Details(int id)
        {
            var ngo = await _db.Ngos.FindAsync(id);
            if (ngo == null) return NotFound();
            ViewBag.Programmes = await _db.Programmes.Where(p => p.NgoId == id && p.IsPublished)
                .OrderByDescending(p => p.StartDate).ToListAsync();
            return View(ngo);
        }
    }
}
