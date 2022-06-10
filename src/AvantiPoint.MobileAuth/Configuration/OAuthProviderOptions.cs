using Microsoft.AspNetCore.Authentication;

namespace AvantiPoint.MobileAuth.Configuration;

internal abstract class OAuthProviderOptions
{
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public abstract void Configure(AuthenticationBuilder builder);
}
