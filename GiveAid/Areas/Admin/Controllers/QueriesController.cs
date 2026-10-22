using System.Net;
using GiveAid.Data;
using GiveAid.Models.ViewModels;
using GiveAid.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Replying to queries raised by users (Help Centre / Contact Us).</summary>
    public class QueriesController : AdminBaseController
    {
        private readonly GiveAidContext _db;
        private readonly IEmailSender _email;

        public QueriesController(GiveAidContext db, IEmailSender email)
        {
            _db = db;
            _email = email;
        }

        // GET: /Admin/Queries?status=Open
        public async Task<IActionResult> Index(string? status = "Open")
        {
            ViewBag.Status = status;
            var query = _db.Queries.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(q => q.Status == status);
            return View(await query.OrderByDescending(q => q.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Reply(int id)
        {
            var q = await _db.Queries.FindAsync(id);
            if (q == null) return NotFound();
            return View(new ReplyViewModel { Id = id, Query = q, Reply = q.Reply ?? "" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ReplyViewModel model)
        {
            var q = await _db.Queries.FindAsync(model.Id);
            if (q == null) return NotFound();
            if (!ModelState.IsValid) { model.Query = q; return View(model); }

            q.Reply = model.Reply.Trim();
            q.Status = "Answered";
            q.RepliedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // e-mail the reply (if SMTP is configured); members also see it on their dashboard
            bool mailed = await _email.SendAsync(q.Email, q.Name, "Re: " + q.Subject,
                $"<p>Dear {WebUtility.HtmlEncode(q.Name)},</p><p>{WebUtility.HtmlEncode(q.Reply).Replace("\n", "<br>")}</p>" +
                $"<hr><p><small>Your question: {WebUtility.HtmlEncode(q.Message)}</small></p><p>Give-AID Help Centre</p>");

            Success(mailed ? "Reply saved and e-mailed." : "Reply saved. (E-mail not sent - SMTP is not configured.)");
            return RedirectToAction(nameof(Index));
        }
    }
}
