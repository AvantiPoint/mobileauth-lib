using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth.Authentication;

public interface ITokenService
{
    ValueTask<string> BuildToken(IEnumerable<Claim> claims);
    ValueTask<bool> IsTokenValid(string token);
    ValueTask InvalidateToken(string token);
    SymmetricSecurityKey GetKey();
}
