using GiveAid.Data;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>All payment transactions, filterable by cause and date.</summary>
    public class DonationsController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public DonationsController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index(int? causeId, DateTime? from, DateTime? to)
        {
            var query = _db.Donations.Include(d => d.Cause).AsQueryable();
            if (causeId.HasValue) query = query.Where(d => d.CauseId == causeId);
            if (from.HasValue) query = query.Where(d => d.DonatedAt >= from.Value.Date);
            if (to.HasValue) query = query.Where(d => d.DonatedAt < to.Value.Date.AddDays(1));

            return View(new DonationReportViewModel
            {
                Donations = await query.OrderByDescending(d => d.DonatedAt).ToListAsync(),
                Causes = await _db.Causes.OrderBy(c => c.DisplayOrder).ToListAsync(),
                CauseId = causeId,
                From = from,
                To = to
            });
        }
    }
}
