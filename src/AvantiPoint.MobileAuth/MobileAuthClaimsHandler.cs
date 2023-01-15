using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AvantiPoint.MobileAuth;

public class MobileAuthClaimsHandler : IMobileAuthClaimsHandler
{
    public virtual ValueTask<IEnumerable<Claim>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme)
    {
        if (auth.Principal is null)
            throw new NullReferenceException("The Authentication Result Principal is null.");

        var claims = GetClaims(auth.Principal);

        claims["provider"] = scheme;

        if(auth.Properties is not null)
        {
            claims["access_token"] = auth.Properties.GetTokenValue("access_token") ?? string.Empty;
            claims["id_token"] = auth.Properties.GetTokenValue("id_token") ?? string.Empty;
            claims["refresh_token"] = auth.Properties.GetTokenValue("refresh_token") ?? string.Empty;
            claims["expires_in"] = (auth.Properties.ExpiresUtc?.ToUnixTimeSeconds() ?? -1).ToString();
        }

        ConfigureName(ref claims);

        return ValueTask.FromResult(claims.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "-1")
            .Select(x => new Claim(x.Key, x.Value)));
    }

    private static void ConfigureName(ref Dictionary<string, string> claims)
    {
        if (claims.TryGetValue("name", out var name) && !string.IsNullOrEmpty(name))
            return;
        else if (claims.TryGetValue("surname", out var surname) && claims.TryGetValue("given_name", out var givenname) && !string.IsNullOrEmpty(surname) && !string.IsNullOrEmpty(givenname))
            claims["name"] = $"{givenname} {surname}".Trim();
    }

    private static Dictionary<string, string> GetClaims(ClaimsPrincipal principal)
    {
        var claims = new Dictionary<string, string>();

        AddClaim(ref claims, "email", principal.FindFirstValue(ClaimTypes.Email));
        AddClaim(ref claims, "name", principal.FindFirstValue(ClaimTypes.Name));
        AddClaim(ref claims, "given_name", principal.FindFirstValue(ClaimTypes.GivenName));
        AddClaim(ref claims, "surname", principal.FindFirstValue(ClaimTypes.Surname));
        AddClaim(ref claims, "provider_id", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        return claims;
    }

    private static void AddClaim(ref Dictionary<string, string> claims, string claim, string? value)
    {
        if (!claims.ContainsKey(claim) && !string.IsNullOrEmpty(value))
            claims[claim] = value;
    }

    protected static string? FindFirstValue(IEnumerable<Claim> claims, string type) =>
        claims.FirstOrDefault(x => x.Type== type)?.Value;
}
