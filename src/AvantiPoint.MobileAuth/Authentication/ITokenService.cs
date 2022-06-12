using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth.Authentication;

public interface ITokenService
{
    string BuildToken(IDictionary<string, string> claims);
    bool IsTokenValid(string token);
    SymmetricSecurityKey GetKey();
}
