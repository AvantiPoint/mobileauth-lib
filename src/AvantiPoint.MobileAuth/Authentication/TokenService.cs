using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth.Authentication;

public class TokenService : ITokenService
{
    private ITokenOptions _options { get; }

    private IHttpContextAccessor _contextAccessor { get; }

    private ILogger _logger { get; }

    public TokenService(IHttpContextAccessor contextAccessor, ILogger<TokenService> logger, ITokenOptions options)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
        _options = options;
    }

    public async ValueTask<string> BuildToken(IDictionary<string, string> userClaims)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(30);
        if (userClaims.ContainsKey("expires_in") &&
            long.TryParse(userClaims["expires_in"], out var expires_in) &&
            expires_in > 0)
            expires = DateTimeOffset.FromUnixTimeSeconds(expires_in);

        var claims = userClaims.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "-1")
            .Select(x => new Claim(x.Key, x.Value));

        var host = GetHost();
        var securityKey = GetKey();
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(host, host, claims,
            expires: expires.UtcDateTime, signingCredentials: credentials);
        var jwt = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        await OnTokenCreated(tokenDescriptor, jwt);
        return jwt;
    }

    protected virtual ValueTask OnTokenCreated(JwtSecurityToken securityToken, string jwt) => ValueTask.CompletedTask;

    public virtual ValueTask<bool> IsTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token))
            return ValueTask.FromResult(false);

        try
        {
            var host = GetHost();
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = host,
                ValidAudience = host,
                IssuerSigningKey = GetKey(),
            }, out SecurityToken validatedToken);

            var now = DateTime.UtcNow;
            return ValueTask.FromResult(now >= validatedToken.ValidFrom && now <= validatedToken.ValidTo);
        }
        catch
        {
            return ValueTask.FromResult(false);
        }
    }

    public virtual ValueTask InvalidateToken(string token) => ValueTask.CompletedTask;

    public SymmetricSecurityKey GetKey()
    {
        var key = _options.JwtKey;
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("No key has been configured. Using default development key. Please provide a configuration value for 'OAuth:JwtKey'.");
            key = GetType().AssemblyQualifiedName;
        }

        return GetKey(key);
    }

    private string GetHost()
    {
        var request = _contextAccessor.HttpContext?.Request;
        if (request is null)
            throw new ArgumentNullException("HttpContext");

        return $"{request.Scheme}://{request.Host.Value}";
    }

    internal static SymmetricSecurityKey GetKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var data = Encoding.UTF8.GetBytes(key);
        return new SymmetricSecurityKey(data);
    }
}
