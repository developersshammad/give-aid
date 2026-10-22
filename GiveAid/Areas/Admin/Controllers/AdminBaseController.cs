using GiveAid.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiveAid.Areas.Admin.Controllers
{
    /// <summary>
    /// Base class for every ADMIN module controller. Only a logged-in administrator
    /// (AdminCookie with role "Admin") can open any page that inherits from this class.
    /// </summary>
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Admin, Roles = "Admin")]
    public abstract class AdminBaseController : Controller
    {
        /// <summary>Shows a green message on the next page.</summary>
        protected void Success(string message) => TempData["Success"] = message;

        /// <summary>Shows a red message on the next page.</summary>
        protected void Error(string message) => TempData["Error"] = message;
    }
}
