using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvantiPoint.MobileAuth.Authentication;

internal sealed class MobileJwtValidationHandler : JwtBearerHandler
{
    private ITokenService _tokenService { get; }

    public MobileJwtValidationHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITokenService tokenService,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _tokenService = tokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var host = $"{Request.Scheme}://{Request.Host.Value}";
        Options.TokenValidationParameters.ValidAudience = host;
        Options.TokenValidationParameters.ValidIssuer = host;
        Options.TokenValidationParameters.IssuerSigningKey = _tokenService.GetKey();

        string header = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(header))
            return AuthenticateResult.NoResult();

        var token = Regex.Replace(header, "Bearer", string.Empty).Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();
        else if (!await _tokenService.IsTokenValid(token))
            return AuthenticateResult.Fail("No valid token found");

        return await base.HandleAuthenticateAsync();
    }
}
