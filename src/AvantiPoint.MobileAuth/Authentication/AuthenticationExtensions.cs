using System.Security.Claims;

namespace AvantiPoint.MobileAuth.Authentication;

internal static class AuthenticationExtensions
{
    public static bool ContainsKey(this IEnumerable<Claim> claims, string type) =>
        claims.Any(x => x.Type == type);

    public static string? FindFirstValue(this IEnumerable<Claim> claims, string type) =>
        claims.FirstOrDefault(x => x.Type == type)?.Value;
}
