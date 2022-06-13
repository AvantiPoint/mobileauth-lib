using AvantiPoint.MobileAuth.Authentication;

namespace AvantiPoint.MobileAuth.Configuration;

internal class OAuthLibraryOptions : ITokenOptions
{
    public string? CallbackScheme { get; set; }

    public string? JwtKey { get; set; }

    public AppleOAuthOptions? Apple { get; set; }

    public GoogleProviderOptions? Google { get; set; }

    public MicrosoftProviderOptions? Microsoft { get; set; }
}
