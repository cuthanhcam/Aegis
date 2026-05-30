using Aegis.Contracts.Authentication;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aegis.Api.Controllers.Helpers
{
    internal static class AuthControllerHelpers
    {
        public static string ResolveRefreshToken(
            HttpRequest request,
            RefreshRequestDto requestBody,
            string cookieName)
        {
            var tokenFromCookie = request.Cookies[cookieName];
            return !string.IsNullOrWhiteSpace(tokenFromCookie)
                ? tokenFromCookie
                : requestBody.RefreshToken ?? string.Empty;
        }

        public static void SetRefreshCookie(
            HttpRequest request,
            HttpResponse response,
            string cookieName,
            string refreshToken,
            string cookiePath,
            TimeSpan refreshTokenLifetime)
        {
            var secure = string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            var sameSite = secure ? SameSiteMode.None : SameSiteMode.Lax;

            response.Cookies.Append(cookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                IsEssential = true,
                Path = cookiePath,
                Expires = DateTimeOffset.UtcNow.Add(refreshTokenLifetime),
            });
        }

        public static void DeleteRefreshCookie(
            HttpResponse response,
            string cookieName,
            string cookiePath)
        {
            response.Cookies.Delete(cookieName, new CookieOptions
            {
                Path = cookiePath,
                SameSite = SameSiteMode.Lax,
            });
        }

        public static UserProfileDto CreateUserProfile(ClaimsPrincipal user)
        {
            var subject = GetClaimValue(user, JwtRegisteredClaimNames.Sub, "sub", ClaimTypes.NameIdentifier);
            var username = GetClaimValue(user, "unique_name", "preferred_username") ?? subject;
            var tenantId = GetClaimValue(user, "tenant_id", "tid") ?? string.Empty;

            var roles = user.FindAll("role")
                .Concat(user.FindAll(ClaimTypes.Role))
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            DateTimeOffset? expiresAt = null;
            var expRaw = GetClaimValue(user, JwtRegisteredClaimNames.Exp, "exp");
            if (long.TryParse(expRaw, out var expSeconds))
            {
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            }

            return new UserProfileDto(subject, username, tenantId, roles, expiresAt);
        }

        private static string GetClaimValue(
            ClaimsPrincipal user,
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = user.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
