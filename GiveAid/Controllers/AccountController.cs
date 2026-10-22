using System.Security.Claims;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Controllers
{
    /// <summary>USER module: register, login, logout and "My account" dashboard.</summary>
    public class AccountController : Controller
    {
        private readonly GiveAidContext _db;
        public AccountController(GiveAidContext db) => _db = db;

        // Options shown in the registration form
        public static readonly string[] Qualifications =
            { "Matric", "Intermediate", "Diploma", "Bachelor's", "Master's", "PhD", "Other" };
        public static readonly string[] Genders = { "Female", "Male", "Other" };

        // GET: /Account/Register
        public async Task<IActionResult> Register()
        {
            if (User.GetMemberId() != null) return RedirectToAction(nameof(Dashboard));
            ViewBag.Causes = await _db.Causes.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).Select(c => c.Name).ToListAsync();
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var email = (model.Email ?? "").Trim().ToLowerInvariant();

            // extra server-side rules
            if (!model.AcceptTerms)
                ModelState.AddModelError(nameof(model.AcceptTerms), "You must accept the terms to register.");
            if (await _db.Members.AnyAsync(m => m.Email == email))
                ModelState.AddModelError(nameof(model.Email), "An account with this e-mail already exists. Please log in.");
            if (model.DateOfBirth.HasValue)
            {
                var dob = model.DateOfBirth.Value.Date;
                if (dob > DateTime.Today.AddYears(-13))
                    ModelState.AddModelError(nameof(model.DateOfBirth), "You must be at least 13 years old to register.");
                else if (dob < DateTime.Today.AddYears(-110))
                    ModelState.AddModelError(nameof(model.DateOfBirth), "Enter a valid date of birth.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Causes = await _db.Causes.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).Select(c => c.Name).ToListAsync();
                return View(model);
            }

            var member = new Member
            {
                FullName = model.FullName.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.Hash(model.Password),
                Phone = model.Phone.Trim(),
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Address = model.Address?.Trim(),
                City = model.City.Trim(),
                Country = model.Country.Trim(),
                Occupation = model.Occupation.Trim(),
                Organization = model.Organization?.Trim(),
                Qualification = model.Qualification,
                AreasOfInterest = string.Join(", ", model.Interests)
            };
            _db.Members.Add(member);
            await _db.SaveChangesAsync();

            await SignInMemberAsync(member, persistent: false);
            TempData["Success"] = $"Welcome to Give-AID, {member.FullName}! Your account is ready.";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.GetMemberId() != null) return RedirectToAction(nameof(Dashboard));
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim().ToLowerInvariant();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email);

            if (member == null || !PasswordHasher.Verify(model.Password, member.PasswordHash))
            {
                ModelState.AddModelError("", "E-mail or password is incorrect.");
                return View(model);
            }
            if (!member.IsActive)
            {
                ModelState.AddModelError("", "Your account has been disabled. Please contact the Help Centre.");
                return View(model);
            }

            await SignInMemberAsync(member, model.RememberMe);

            // only redirect to local URLs (prevents open-redirect attacks)
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: /Account/Logout
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthSchemes.Member);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Dashboard  - member's own donations, queries, programmes and invitations
        [Authorize(AuthenticationSchemes = AuthSchemes.Member, Roles = "Member")]
        public async Task<IActionResult> Dashboard()
        {
            int id = User.GetMemberId()!.Value;
            var member = await _db.Members.FindAsync(id);
            if (member == null)
            {
                await HttpContext.SignOutAsync(AuthSchemes.Member);
                return RedirectToAction(nameof(Login));
            }

            var vm = new MemberDashboardViewModel
            {
                Member = member,
                Donations = await _db.Donations.Include(d => d.Cause)
                    .Where(d => d.MemberId == id).OrderByDescending(d => d.DonatedAt).ToListAsync(),
                Queries = await _db.Queries.Where(q => q.MemberId == id).OrderByDescending(q => q.CreatedAt).ToListAsync(),
                Interests = await _db.ProgrammeInterests.Include(i => i.Programme)
                    .Where(i => i.MemberId == id).OrderByDescending(i => i.SubmittedAt).ToListAsync(),
                Invitations = await _db.Invitations.Where(i => i.MemberId == id).OrderByDescending(i => i.SentAt).ToListAsync()
            };
            return View(vm);
        }

        /// <summary>Creates the login cookie with the member's Id, name, e-mail and role.</summary>
        private async Task SignInMemberAsync(Member member, bool persistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, member.Id.ToString()),
                new(ClaimTypes.Name, member.FullName),
                new(ClaimTypes.Email, member.Email),
                new(ClaimTypes.Role, "Member")
            };
            var identity = new ClaimsIdentity(claims, AuthSchemes.Member);
            await HttpContext.SignInAsync(AuthSchemes.Member, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = persistent });
        }
    }
}
