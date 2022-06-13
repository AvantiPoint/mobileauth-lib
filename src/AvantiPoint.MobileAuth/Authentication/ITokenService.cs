using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth.Authentication;

public interface ITokenService
{
    ValueTask<string> BuildToken(IDictionary<string, string> claims);
    ValueTask<bool> IsTokenValid(string token);
    SymmetricSecurityKey GetKey();
}
