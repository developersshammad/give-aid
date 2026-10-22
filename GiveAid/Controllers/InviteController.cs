using System.Net;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using GiveAid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>Members invite friends to join the cause by e-mail.</summary>
    [Authorize(AuthenticationSchemes = AuthSchemes.Member, Roles = "Member")]
    public class InviteController : Controller
    {
        private readonly GiveAidContext _db;
        private readonly IEmailSender _email;

        public InviteController(GiveAidContext db, IEmailSender email)
        {
            _db = db;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new InviteViewModel
            {
                PersonalMessage = "I donate and volunteer through Give-AID. It's simple and you choose exactly which cause your money goes to. Join me!",
                Sent = await SentListAsync()
            };
            ViewBag.SmtpConfigured = _email.IsConfigured;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(InviteViewModel model)
        {
            int memberId = User.GetMemberId()!.Value;
            var friendEmail = (model.FriendEmail ?? "").Trim().ToLowerInvariant();

            if (friendEmail == User.GetEmail().ToLowerInvariant())
                ModelState.AddModelError(nameof(model.FriendEmail), "You cannot invite yourself.");
            if (await _db.Members.AnyAsync(m => m.Email == friendEmail))
                ModelState.AddModelError(nameof(model.FriendEmail), "This person is already a Give-AID member.");

            if (!ModelState.IsValid)
            {
                model.Sent = await SentListAsync();
                ViewBag.SmtpConfigured = _email.IsConfigured;
                return View(model);
            }

            // build the invitation mail
            string sender = User.GetDisplayName();
            string link = Url.Action("Register", "Account", null, Request.Scheme)!;
            string subject = $"{sender} invites you to join Give-AID";
            string body =
                $"<p>Dear {WebUtility.HtmlEncode(model.FriendName)},</p>" +
                $"<p>{WebUtility.HtmlEncode(model.PersonalMessage ?? "")}</p>" +
                $"<p><a href=\"{link}\">Create your free Give-AID account</a></p>" +
                $"<p>- {WebUtility.HtmlEncode(sender)}</p>";

            bool sent = await _email.SendAsync(friendEmail, model.FriendName, subject, body);

            _db.Invitations.Add(new Invitation
            {
                MemberId = memberId,
                FriendName = model.FriendName.Trim(),
                FriendEmail = friendEmail,
                PersonalMessage = model.PersonalMessage?.Trim(),
                EmailSent = sent
            });
            await _db.SaveChangesAsync();

            if (sent)
                TempData["Success"] = $"Invitation e-mailed to {model.FriendName}.";
            else
            {
                // no SMTP server: give the member a ready-made mail link instead
                TempData["Success"] = $"Invitation for {model.FriendName} saved.";
                TempData["MailTo"] = $"mailto:{friendEmail}?subject={Uri.EscapeDataString(subject)}" +
                                     $"&body={Uri.EscapeDataString($"Dear {model.FriendName},\n\n{model.PersonalMessage}\n\nJoin here: {link}\n\n- {sender}")}";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<Invitation>> SentListAsync()
        {
            int memberId = User.GetMemberId()!.Value;
            return await _db.Invitations.Where(i => i.MemberId == memberId)
                .OrderByDescending(i => i.SentAt).ToListAsync();
        }
    }
}
