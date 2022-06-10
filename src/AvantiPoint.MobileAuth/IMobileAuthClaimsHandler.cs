using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AvantiPoint.MobileAuth;

public interface IMobileAuthClaimsHandler
{
    ValueTask<Dictionary<string, string>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme);
}
