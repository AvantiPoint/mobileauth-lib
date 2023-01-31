using AvantiPoint.MobileAuth.Authentication;

namespace AvantiPoint.MobileAuth.Configuration;

internal class OAuthLibraryOptions : ITokenOptions
{
    public string? AuthPath { get; set; }

    public string? CallbackScheme { get; set; }

    public string? JwtKey { get; set; }

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    public bool OverrideTokenExpiration { get; set; }

    public AppleOAuthOptions? Apple { get; set; }

    public GoogleProviderOptions? Google { get; set; }

    public MicrosoftProviderOptions? Microsoft { get; set; }

    internal string Signin => $"/{SanitizePath()}/signin";

    internal string Signout => $"/{SanitizePath()}/signout";

    internal string Refresh => $"/{SanitizePath()}/refresh";

    private string SanitizePath()
    {
        var path = AuthPath;
        if (!string.IsNullOrEmpty(path))
        {
            if (path.EndsWith('/'))
                path = path.Substring(0, path.Length - 1);

            if (path.StartsWith('/'))
                path = path.Substring(1);
        }

        if (string.IsNullOrEmpty(path))
            path = "mobileauth";

        return path;
    }
}
