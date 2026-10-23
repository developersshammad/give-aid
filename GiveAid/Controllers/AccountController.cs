using System.Security.Claims;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models;
using GiveAid.Models.ViewModels;
using GiveAid.Services;
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
        private readonly OtpService _otp;
        public AccountController(GiveAidContext db, OtpService otp)
        {
            _db = db;
            _otp = otp;
        }

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
                AreasOfInterest = string.Join(", ", model.Interests),
                EmailVerified = !(_otp.Options.Enabled && _otp.EmailWorks)   // must type the e-mailed code first
            };
            _db.Members.Add(member);
            await _db.SaveChangesAsync();

            if (!member.EmailVerified)
            {
                await SendVerifyCodeAsync(member);
                return RedirectToAction(nameof(VerifyEmail), new { email = member.Email });
            }
            await SignInMemberAsync(member, persistent: false);
            TempData["Success"] = $"Welcome to Give-AID, {member.FullName}! Your account is ready.";
            return RedirectToAction(nameof(Dashboard));
        }

        // =============================================================
        //  E-mail verification with a one-time code (OTP)
        // =============================================================

        /// <summary>Sends a code and puts the right message on the next page.</summary>
        private async Task SendVerifyCodeAsync(Member member)
        {
            var r = await _otp.SendAsync(member.Email, member.FullName, "verify");
            if (r.Emailed) TempData["Success"] = $"We sent a 6-digit code to {member.Email}. It works for {_otp.Options.CodeMinutes} minutes.";
            else TempData["Error"] = r.Error ?? "The code could not be e-mailed. Please try again later or contact the Help Centre.";
        }

        // GET: /Account/VerifyEmail?email=...
        public async Task<IActionResult> VerifyEmail(string? email, string? returnUrl = null, bool remember = false)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email);
            if (member == null) return RedirectToAction(nameof(Login));
            if (member.EmailVerified) { TempData["Success"] = "Your e-mail is already verified. Please log in."; return RedirectToAction(nameof(Login)); }
            ViewBag.Wait = await _otp.WaitSecondsAsync(email, "verify");
            ViewBag.EmailWorks = _otp.EmailWorks;
            return View(new VerifyEmailViewModel { Email = email, ReturnUrl = returnUrl, Remember = remember });
        }

        // POST: /Account/VerifyEmail
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            var email = (model.Email ?? "").Trim().ToLowerInvariant();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email);
            if (member == null) return RedirectToAction(nameof(Login));
            if (member.EmailVerified) return RedirectToAction(nameof(Login));

            if (ModelState.IsValid)
            {
                var (result, left) = await _otp.CheckAsync(email, "verify", model.Code);
                if (result == OtpCheck.Ok)
                {
                    member.EmailVerified = true;
                    await _db.SaveChangesAsync();
                    if (!member.IsActive) { TempData["Success"] = "E-mail verified."; return RedirectToAction(nameof(Login)); }
                    await SignInMemberAsync(member, model.Remember);
                    TempData["Success"] = $"E-mail verified. Welcome to Give-AID, {member.FullName}!";
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
                    return RedirectToAction(nameof(Dashboard));
                }
                ModelState.AddModelError(nameof(model.Code), OtpService.Message(result, left));
            }
            ViewBag.Wait = await _otp.WaitSecondsAsync(email, "verify");
            ViewBag.EmailWorks = _otp.EmailWorks;
            model.Code = "";
            return View(model);
        }

        // POST: /Account/ResendCode
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode(string email, string? returnUrl, bool remember = false)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email);
            if (member == null || member.EmailVerified) return RedirectToAction(nameof(Login));
            await SendVerifyCodeAsync(member);
            return RedirectToAction(nameof(VerifyEmail), new { email, returnUrl, remember });
        }

        // =============================================================
        //  Forgot password - a reset code is e-mailed (never shown on screen)
        // =============================================================

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            ViewBag.EmailWorks = _otp.EmailWorks;
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewBag.EmailWorks = _otp.EmailWorks;
            if (!ModelState.IsValid) return View(model);
            if (!_otp.EmailWorks)
            {
                ModelState.AddModelError("", "Password reset by e-mail is not available because e-mail is not set up on this website. Please raise a query in the Help Centre.");
                return View(model);
            }
            var email = model.Email.Trim().ToLowerInvariant();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email && m.IsActive);
            if (member != null)
            {
                var r = await _otp.SendAsync(email, member.FullName, "reset");
                if (r.Error != null) { ModelState.AddModelError("", r.Error); return View(model); }
            }
            // same message whether or not the account exists, so e-mail addresses cannot be guessed
            TempData["Success"] = $"If an account exists for {email}, a 6-digit code has been sent to it.";
            return RedirectToAction(nameof(ResetPassword), new { email });
        }

        // GET: /Account/ResetPassword?email=...
        public IActionResult ResetPassword(string? email) =>
            View(new ResetPasswordViewModel { Email = (email ?? "").Trim().ToLowerInvariant() });

        // POST: /Account/ResetPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var email = model.Email.Trim().ToLowerInvariant();
            var (result, left) = await _otp.CheckAsync(email, "reset", model.Code);
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Email == email && m.IsActive);
            if (result != OtpCheck.Ok || member == null)
            {
                ModelState.AddModelError(nameof(model.Code), result == OtpCheck.Ok ? "That code is not correct." : OtpService.Message(result, left));
                model.Code = "";
                return View(model);
            }
            member.PasswordHash = PasswordHasher.Hash(model.Password);
            member.EmailVerified = true;                  // the code proves the e-mail belongs to them
            await _db.SaveChangesAsync();
            TempData["Success"] = "Your password has been changed. Please log in with the new password.";
            return RedirectToAction(nameof(Login));
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

            if (!member.EmailVerified && _otp.EmailWorks)
            {
                await SendVerifyCodeAsync(member);
                return RedirectToAction(nameof(VerifyEmail), new { email = member.Email, returnUrl = model.ReturnUrl, remember = model.RememberMe });
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
