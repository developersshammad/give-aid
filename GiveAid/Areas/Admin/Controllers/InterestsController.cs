using GiveAid.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Users who sent their interest to take part in upcoming programmes.</summary>
    public class InterestsController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public InterestsController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index(int? programmeId)
        {
            ViewBag.Programmes = await _db.Programmes.OrderByDescending(p => p.StartDate).ToListAsync();
            ViewBag.ProgrammeId = programmeId;
            var query = _db.ProgrammeInterests.Include(i => i.Programme).AsQueryable();
            if (programmeId.HasValue) query = query.Where(i => i.ProgrammeId == programmeId);
            return View(await query.OrderByDescending(i => i.SubmittedAt).ToListAsync());
        }
    }
}
