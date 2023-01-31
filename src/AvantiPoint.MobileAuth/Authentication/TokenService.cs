using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AvantiPoint.MobileAuth.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth.Authentication;

public class TokenService : ITokenService
{
    private ITokenOptions _options { get; }

    private ITokenStore _tokenStore { get; }

    private IHttpContextAccessor _contextAccessor { get; }

    private ILogger _logger { get; }

    public TokenService(IHttpContextAccessor contextAccessor, ILogger<TokenService> logger, ITokenStore tokenStore, ITokenOptions options)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
        _options = options;
        _tokenStore = tokenStore;
    }

    public async ValueTask<string> BuildToken(IEnumerable<Claim> userClaims)
    {
        var defaultExpiration = _options.DefaultExpiration == default ? TimeSpan.FromMinutes(30) : _options.DefaultExpiration;

        var expires = DateTimeOffset.UtcNow.Add(defaultExpiration);
        if (userClaims.ContainsKey("expires_in") &&
            long.TryParse(userClaims.FindFirstValue("expires_in"), out var expires_in) &&
            expires_in > 0)
        {
            if(_options.OverrideTokenExpiration)
                expires = DateTimeOffset.FromUnixTimeSeconds(expires_in);
        }
        else if(_options.OverrideTokenExpiration)
        {
            _logger.LogInformation("Unable to override token expiration. The provided OAuth token does not have a valid `expires_in` claim.");
        }

        var claims = userClaims.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "-1");

        var host = GetHost();
        _logger.LogInformation($"Using '{host}' for the token host.");
        var securityKey = GetKey();
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(host, host, claims,
            expires: expires.UtcDateTime, signingCredentials: credentials);
        var jwt = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        await _tokenStore.AddToken(jwt, expires);
        await OnTokenCreated(tokenDescriptor, jwt);
        return jwt;
    }

    protected virtual ValueTask OnTokenCreated(JwtSecurityToken securityToken, string jwt) => ValueTask.CompletedTask;

    public virtual async ValueTask<bool> IsTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            if (!await _tokenStore.TokenExists(token))
                return false;

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
            return now >= validatedToken.ValidFrom && now <= validatedToken.ValidTo;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask InvalidateToken(string token)
    {
        _logger.LogInformation("Invalidating Token.");
        await _tokenStore.RemoveToken(token);
        _logger.LogInformation("Token Invalidated.");
        await OnTokenInvalidated(token);
    }

    protected virtual ValueTask OnTokenInvalidated(string token) => ValueTask.CompletedTask;

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
