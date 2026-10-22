using System.Security.Claims;
using GiveAid.Data;
using GiveAid.Helpers;
using GiveAid.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>Admin login / logout (separate from member login).</summary>
    [Area("Admin")]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly GiveAidContext _db;
        public AuthController(GiveAidContext db) => _db = db;

        public IActionResult Login(string? returnUrl = null) =>
            View(new AdminLoginViewModel { ReturnUrl = returnUrl });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Username OR e-mail, not case-sensitive (phones often type a capital first letter)
            var who = model.Username.Trim().ToLower();
            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username.ToLower() == who || a.Email.ToLower() == who);
            if (admin == null || !PasswordHasher.Verify(model.Password, admin.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new(ClaimTypes.Name, admin.FullName),
                new(ClaimTypes.Email, admin.Email),
                new(ClaimTypes.Role, "Admin")
            };
            await HttpContext.SignInAsync(AuthSchemes.Admin,
                new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSchemes.Admin)));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(AuthSchemes.Admin);
            return RedirectToAction(nameof(Login));
        }
    }
}
