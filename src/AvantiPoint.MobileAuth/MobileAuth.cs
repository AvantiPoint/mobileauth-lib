using System.Net;
using AvantiPoint.MobileAuth.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AvantiPoint.MobileAuth;

public static class MobileAuth
{
    public static WebApplicationBuilder AddMobileAuth(this WebApplicationBuilder appBuilder) =>
        appBuilder.AddMobileAuth(_ => { });

    public static WebApplicationBuilder AddMobileAuth(this WebApplicationBuilder appBuilder, Action<AuthenticationBuilder> configureAuthenticationBuilder)
    {
        var options = appBuilder.Configuration
            .GetSection("OAuth")
            .Get<OAuthLibraryOptions>();

        if (string.IsNullOrEmpty(options.CallbackScheme))
            throw new ArgumentNullException(nameof(OAuthLibraryOptions.CallbackScheme));

        var authBuilder = appBuilder.Services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie();
        options.Apple?.Configure(authBuilder, appBuilder);
        options.Google?.Configure(authBuilder);
        options.Microsoft?.Configure(authBuilder);

        appBuilder.Services.AddAuthorization();
        appBuilder.Services.AddSingleton(options);
        appBuilder.Services.TryAddScoped<IMobileAuthClaimsHandler, MobileAuthClaimsHandler>();

        configureAuthenticationBuilder(authBuilder);

        return appBuilder;
    }

    public static WebApplication MapMobileAuthRoute(this WebApplication app, string routePrefix = "mobileauth", string name = "signin")
    {
        app.MapGet($"{routePrefix}/{{scheme}}", Signin)
           .Produces(302)
           .ProducesProblem(204)
           .ProducesProblem(404)
           .WithName(name)
           .WithDisplayName(name);

        return app;
    }

    private static async Task Signin(string scheme, HttpContext context)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await provider.GetAllSchemesAsync();
        if(schemes is null || !schemes.Any())
        {
            context.Response.StatusCode = 204;
            context.Response.Headers.Add("Status", "No Authentication Schemes are configured");
            return;
        }

        var authenticationScheme = schemes.FirstOrDefault(x => x.Name.Equals(scheme, StringComparison.InvariantCultureIgnoreCase));

        if(authenticationScheme is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        var auth = await context.AuthenticateAsync(authenticationScheme.Name);

        if (!auth.Succeeded
            || auth?.Principal == null
            || !auth.Principal.Identities.Any(id => id.IsAuthenticated)
            || string.IsNullOrEmpty(auth.Properties.GetTokenValue("access_token")))
        {
            // Not authenticated, challenge
            await context.ChallengeAsync(scheme);
            return;
        }

        var handler = context.RequestServices.GetRequiredService<IMobileAuthClaimsHandler>();
        var claims = await handler.GenerateClaims(context, auth, scheme);

        var qs = claims.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "-1")
            .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}");

        // Build the result url
        var options = context.RequestServices.GetRequiredService<OAuthLibraryOptions>();
        var url = $"{options.CallbackScheme}://#{string.Join("&", qs)}";

        // Redirect to final url
        context.Response.Redirect(url);
    }
}
