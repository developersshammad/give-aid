using GiveAid.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.ViewComponents
{
    /// <summary>Builds the "About Us" drop-down menu from the pages in the database,
    /// so when the admin renames a page the menu changes too.</summary>
    public class AboutMenuViewComponent : ViewComponent
    {
        private readonly GiveAidContext _db;
        public AboutMenuViewComponent(GiveAidContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync() =>
            View(await _db.ContentPages.Where(p => p.Section == "About")
                .OrderBy(p => p.DisplayOrder).ToListAsync());
    }
}
