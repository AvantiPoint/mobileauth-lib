using System.Security.Claims;
using AvantiPoint.MobileAuth;
using DemoAPI.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

// If needed provide this as a Scoped Service.
public class CustomClaimsHandler : MobileAuthClaimsHandler
{
    private UserContext _userContext { get; }
    public CustomClaimsHandler(UserContext userContext)
    {
        _userContext = userContext;
    }

    public override async ValueTask<IEnumerable<Claim>> GenerateClaims(HttpContext context, AuthenticateResult auth, string scheme)
    {
        var claims = (await base.GenerateClaims(context, auth, scheme).ConfigureAwait(false)).ToList();
        var email = FindFirstValue(claims, "email");
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("The claims do not contain an email claim");

        // Need to update a database or specify specific claims? You can do that here...
        var userRoles = await _userContext.UserRoles.Where(x => x.Email == email).ToArrayAsync().ConfigureAwait(false);
        if(!userRoles.Any())
        {
            userRoles = new UserRole[] { new UserRole { Email = email, Role = "GenericUser" } };
            await _userContext.UserRoles.AddRangeAsync(userRoles).ConfigureAwait(false);
            await _userContext.SaveChangesAsync().ConfigureAwait(false);
        }

        claims.AddRange(userRoles.Select(x => new Claim(ClaimTypes.Role, x.Role)));

        return claims;
    }
}
