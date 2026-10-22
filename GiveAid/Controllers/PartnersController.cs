using GiveAid.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>"Our Partners" - companies associated with Give-AID, shown with logos.</summary>
    public class PartnersController : Controller
    {
        private readonly GiveAidContext _db;
        public PartnersController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Partners.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToListAsync());
    }
}
