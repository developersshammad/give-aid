using System.Security.Claims;

namespace GiveAid.Helpers
{
    /// <summary>Names of the two login cookies (see Program.cs).</summary>
    public static class AuthSchemes
    {
        public const string Member = "MemberCookie";
        public const string Admin = "AdminCookie";
    }

    /// <summary>Small helpers to read the logged-in user's data from the cookie claims.</summary>
    public static class ClaimsExtensions
    {
        /// <summary>Returns the database Id of the logged-in member, or null for guests.</summary>
        public static int? GetMemberId(this ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true) return null;
            if (!user.IsInRole("Member")) return null;
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        public static string GetDisplayName(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Name) ?? "";

        public static string GetEmail(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Email) ?? "";
    }
}
