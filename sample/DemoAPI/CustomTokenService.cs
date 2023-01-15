using System.IdentityModel.Tokens.Jwt;
using AvantiPoint.MobileAuth.Authentication;
using DemoAPI.Data;
using Microsoft.EntityFrameworkCore;

public class CustomTokenService : TokenService
{
    private UserContext _userContext { get; }
    public CustomTokenService(UserContext userContext, IHttpContextAccessor contextAccessor, ILogger<TokenService> logger, ITokenOptions options) 
        : base(contextAccessor, logger, options)
    {
        _userContext = userContext;
    }

    protected override async ValueTask OnTokenCreated(JwtSecurityToken securityToken, string jwt)
    {
        await _userContext.AuthorizedTokens.AddAsync(new AuthorizedTokens { Token = jwt });
        await _userContext.SaveChangesAsync();
    }

    public override async ValueTask<bool> IsTokenValid(string token) =>
        await base.IsTokenValid(token) && await _userContext.AuthorizedTokens.AnyAsync(x => x.Token == token);

    public override async ValueTask InvalidateToken(string token)
    {
        var authorized = await _userContext.AuthorizedTokens.FirstOrDefaultAsync(x => x.Token == token);
        if(authorized is not null)
        {
            _userContext.AuthorizedTokens.Remove(authorized);
            await _userContext.SaveChangesAsync();
        }
    }
}