using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AvantiPoint.MobileAuth;

public interface IMobileAuthClaimsHandler
{
    ValueTask<IEnumerable<Claim>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme);
}
