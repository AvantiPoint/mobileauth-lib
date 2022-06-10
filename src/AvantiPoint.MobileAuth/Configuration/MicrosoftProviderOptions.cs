using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AvantiPoint.MobileAuth.Configuration;

internal sealed class MicrosoftProviderOptions : OAuthProviderOptions
{
    public override void Configure(AuthenticationBuilder builder)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            return;

        builder.AddMicrosoftAccount(options =>
        {
            options.ClientId = ClientId;
            options.ClientSecret = ClientSecret;
            options.SaveTokens = true;
        });
    }
}
