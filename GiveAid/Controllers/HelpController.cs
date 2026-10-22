using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>Help Centre: FAQ text (edited by admin) + "Raise a query" form.</summary>
    public class HelpController : Controller
    {
        private readonly GiveAidContext _db;
        public HelpController(GiveAidContext db) => _db = db;

        public async Task<IActionResult> Index() => View(await BuildAsync(null));

        // POST: /Help/Ask
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([Bind(Prefix = "Form")] QueryViewModel form)
        {
            if (!ModelState.IsValid) return View(nameof(Index), await BuildAsync(form));

            _db.Queries.Add(new SupportQuery
            {
                MemberId = User.GetMemberId(),
                Name = form.Name.Trim(),
                Email = form.Email.Trim(),
                Subject = form.Subject.Trim(),
                Message = form.Message.Trim()
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your query has been received. You will see the reply here (members) and by e-mail.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<HelpViewModel> BuildAsync(QueryViewModel? form)
        {
            var vm = new HelpViewModel
            {
                Page = await _db.ContentPages.FirstOrDefaultAsync(p => p.Slug == "help-centre")
                       ?? new ContentPage { Title = "Help Centre", Body = "" },
                Form = form ?? new QueryViewModel()
            };
            var memberId = User.GetMemberId();
            if (memberId != null)
            {
                if (form == null)
                {
                    vm.Form.Name = User.GetDisplayName();
                    vm.Form.Email = User.GetEmail();
                }
                vm.MyQueries = await _db.Queries.Where(q => q.MemberId == memberId)
                    .OrderByDescending(q => q.CreatedAt).ToListAsync();
            }
            return vm;
        }
    }
}
