using GiveAid.Data;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>View all registered members of the website.</summary>
    public class MembersController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public MembersController(GiveAidContext db) => _db = db;

        // GET: /Admin/Members?search=ali
        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Search = search;
            var query = _db.Members.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(m => m.FullName.Contains(s) || m.Email.Contains(s) ||
                                         (m.City != null && m.City.Contains(s)) ||
                                         (m.Occupation != null && m.Occupation.Contains(s)));
            }
            ViewBag.Totals = await _db.Donations.Where(d => d.MemberId != null)
                .GroupBy(d => d.MemberId!.Value)
                .Select(g => new { g.Key, Total = g.Sum(d => d.Amount) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
            return View(await query.OrderByDescending(m => m.RegisteredAt).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var member = await _db.Members.FindAsync(id);
            if (member == null) return NotFound();
            return View(new MemberDetailsViewModel
            {
                Member = member,
                Donations = await _db.Donations.Include(d => d.Cause).Where(d => d.MemberId == id)
                    .OrderByDescending(d => d.DonatedAt).ToListAsync(),
                Invitations = await _db.Invitations.Where(i => i.MemberId == id).OrderByDescending(i => i.SentAt).ToListAsync(),
                Interests = await _db.ProgrammeInterests.Include(i => i.Programme).Where(i => i.MemberId == id).ToListAsync()
            });
        }

        // Enable / disable a member account
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var member = await _db.Members.FindAsync(id);
            if (member == null) return NotFound();
            member.IsActive = !member.IsActive;
            await _db.SaveChangesAsync();
            Success($"{member.FullName}'s account is now {(member.IsActive ? "active" : "disabled")}.");
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
