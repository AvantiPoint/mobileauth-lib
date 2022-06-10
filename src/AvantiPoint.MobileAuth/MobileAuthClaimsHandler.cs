using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AvantiPoint.MobileAuth;

public class MobileAuthClaimsHandler : IMobileAuthClaimsHandler
{
    public virtual ValueTask<Dictionary<string, string>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme)
    {
        var claims = GetClaims(auth);

        claims["provider"] = scheme;

        if(auth.Properties is not null)
        {
            claims["access_token"] = auth.Properties.GetTokenValue("access_token") ?? string.Empty;
            claims["refresh_token"] = auth.Properties.GetTokenValue("refresh_token") ?? string.Empty;
            claims["expires_in"] = (auth.Properties.ExpiresUtc?.ToUnixTimeSeconds() ?? -1).ToString();
        }

        ConfigureName(ref claims);

        return ValueTask.FromResult(claims);
    }

    private static void ConfigureName(ref Dictionary<string, string> claims)
    {
        if (claims.TryGetValue("name", out var name) && !string.IsNullOrEmpty(name))
            return;
        else if (claims.TryGetValue("surname", out var surname) && claims.TryGetValue("given_name", out var givenname) && !string.IsNullOrEmpty(surname) && !string.IsNullOrEmpty(givenname))
            claims["name"] = $"{givenname} {surname}".Trim();
    }

    private static Dictionary<string, string> GetClaims(AuthenticateResult auth) =>
        new()
        {
            { "email", auth.Principal.FindFirstValue(ClaimTypes.Email) },
            { "name", auth.Principal.FindFirstValue(ClaimTypes.Name) },
            { "given_name", auth.Principal.FindFirstValue(ClaimTypes.GivenName) },
            { "surname", auth.Principal.FindFirstValue(ClaimTypes.Surname) },
            { "provider_id", auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier) }
        };
}
