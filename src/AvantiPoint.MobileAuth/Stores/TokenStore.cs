using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace AvantiPoint.MobileAuth.Stores;

internal class TokenStore : DbContext, ITokenStore
{
    public TokenStore(DbContextOptions<TokenStore> options)
        : base(options)
    {
        Tokens = Set<GeneratedToken>();
    }

    public DbSet<GeneratedToken> Tokens { get; }

    public async ValueTask AddToken(string jwt, DateTimeOffset expires)
    {
        await Tokens.AddAsync(new GeneratedToken
        {
            Token = jwt,
            Expires = expires
        });
        await SaveChangesAsync();
    }

    public async ValueTask RemoveToken(string jwt)
    {
        var tokens = await Tokens.Where(x => x.Token == jwt).ToArrayAsync();
        if(tokens.Any())
        {
            Tokens.RemoveRange(tokens);
            await SaveChangesAsync();
        }
    }

    public async ValueTask<bool> TokenExists(string jwt) =>
        await Tokens.AnyAsync(x => x.Token == jwt && x.Expires > DateTimeOffset.Now);
}
