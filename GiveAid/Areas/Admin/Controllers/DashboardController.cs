using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Admin home: totals and latest activity.</summary>
    public class DashboardController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        public DashboardController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var vm = new AdminDashboardViewModel
            {
                TotalRaised = await _db.Donations.SumAsync(d => (decimal?)d.Amount) ?? 0,
                RaisedThisMonth = await _db.Donations.Where(d => d.DonatedAt >= monthStart).SumAsync(d => (decimal?)d.Amount) ?? 0,
                DonationCount = await _db.Donations.CountAsync(),
                MemberCount = await _db.Members.CountAsync(),
                NgoCount = await _db.Ngos.CountAsync(),
                UpcomingProgrammes = await _db.Programmes.CountAsync(p => p.IsPublished && (p.EndDate ?? p.StartDate) >= today),
                OpenQueries = await _db.Queries.CountAsync(q => q.Status == "Open"),
                InterestCount = await _db.ProgrammeInterests.CountAsync(),
                RecentDonations = await _db.Donations.Include(d => d.Cause).OrderByDescending(d => d.DonatedAt).Take(8).ToListAsync(),
                LatestQueries = await _db.Queries.Where(q => q.Status == "Open").OrderByDescending(q => q.CreatedAt).Take(5).ToListAsync(),
                Causes = await CauseStats.LoadAsync(_db, activeOnly: false),

                AdminName = User.GetDisplayName(),
                NewMembersThisMonth = await _db.Members.CountAsync(m => m.RegisteredAt >= monthStart),
                PartnerCount = await _db.Partners.CountAsync(),
                NextProgrammes = await _db.Programmes.Where(p => p.IsPublished && (p.EndDate ?? p.StartDate) >= today)
                    .OrderBy(p => p.StartDate).Take(4).ToListAsync(),
                InterestsPerProgramme = await _db.ProgrammeInterests.GroupBy(i => i.ProgrammeId)
                    .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count),
                NewestMembers = await _db.Members.OrderByDescending(m => m.RegisteredAt).Take(5).ToListAsync()
            };
            // open queries for the side list: oldest first, so nothing waits too long
            vm.LatestQueries = await _db.Queries.Where(q => q.Status == "Open").OrderBy(q => q.CreatedAt).Take(5).ToListAsync();
            vm.OldestOpenQuery = vm.LatestQueries.FirstOrDefault()?.CreatedAt;
            vm.RecentDonations = vm.RecentDonations.Take(6).ToList();

            // totals for the last 12 weeks (Monday to Sunday) for the column chart
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var firstWeek = monday.AddDays(-7 * 11);
            var recent = await _db.Donations.Where(d => d.DonatedAt >= firstWeek)
                .Select(d => new { d.DonatedAt, d.Amount }).ToListAsync();
            for (int i = 0; i < 12; i++)
            {
                var start = firstWeek.AddDays(7 * i);
                var inWeek = recent.Where(d => d.DonatedAt >= start && d.DonatedAt < start.AddDays(7)).ToList();
                vm.Weeks.Add(new WeekTotal { Start = start, Total = inWeek.Sum(d => d.Amount), Count = inWeek.Count, IsCurrent = i == 11 });
            }
            return View(vm);
        }
    }
}
