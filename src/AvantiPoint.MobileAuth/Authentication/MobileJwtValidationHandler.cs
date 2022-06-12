using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvantiPoint.MobileAuth.Authentication;

public class MobileJwtValidationHandler : JwtBearerHandler
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
        return await base.HandleAuthenticateAsync();
    }
}
