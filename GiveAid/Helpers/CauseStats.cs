using GiveAid.Data;
using GiveAid.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Helpers
{
    /// <summary>Calculates amount raised and number of donors for every cause.</summary>
    public static class CauseStats
    {
        public static async Task<List<CauseProgress>> LoadAsync(GiveAidContext db, bool activeOnly)
        {
            var causes = await db.Causes
                .Where(c => !activeOnly || c.IsActive)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .ToListAsync();

            // one grouped query instead of one query per cause
            var totals = await db.Donations
                .GroupBy(d => d.CauseId)
                .Select(g => new { CauseId = g.Key, Raised = g.Sum(d => d.Amount), Donors = g.Count() })
                .ToListAsync();

            return causes.Select(c =>
            {
                var t = totals.FirstOrDefault(x => x.CauseId == c.Id);
                return new CauseProgress { Cause = c, Raised = t?.Raised ?? 0, Donors = t?.Donors ?? 0 };
            }).ToList();
        }
    }
}
