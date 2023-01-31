using Microsoft.EntityFrameworkCore;

namespace AvantiPoint.MobileAuth.Stores;

public interface ITokenStore
{
    ValueTask AddToken(string jwt, DateTimeOffset expires);
    ValueTask<bool> TokenExists(string jwt);
    ValueTask RemoveToken(string jwt);
}
