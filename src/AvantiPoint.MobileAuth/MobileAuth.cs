using System.Net;
using AvantiPoint.MobileAuth.Authentication;
using AvantiPoint.MobileAuth.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.LoginPath = "/mobileauth/";
            });
        authBuilder.AddScheme<JwtBearerOptions, MobileJwtValidationHandler>(JwtBearerDefaults.AuthenticationScheme, null, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };
            })
            .Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
        options.Apple?.Configure(authBuilder, appBuilder);
        options.Google?.Configure(authBuilder);
        options.Microsoft?.Configure(authBuilder);

        appBuilder.Services.AddAuthorization()
            .AddSingleton(options)
            .AddHttpContextAccessor();

        appBuilder.Services.TryAddScoped<IMobileAuthClaimsHandler, MobileAuthClaimsHandler>();
        appBuilder.Services.TryAddScoped<ITokenService, TokenService>();

        configureAuthenticationBuilder(authBuilder);

        return appBuilder;
    }

    public static WebApplication MapMobileAuthRoute(this WebApplication app, string routePrefix = "mobileauth", string name = "signin")
    {
        if (routePrefix.EndsWith('/'))
            routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);

        app.MapGet($"{routePrefix}/{{scheme}}", Signin)
           .Produces(302)
           .ProducesProblem(204)
           .ProducesProblem(404)
           .AllowAnonymous()
           .WithName(name)
           .WithDisplayName(name);

        return app;
    }

    private static async Task Signin(string scheme, HttpContext context)
    {
        if(scheme.Equals(CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.InvariantCultureIgnoreCase) ||
            scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.InvariantCultureIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Headers.Add("Status", "Unsupported Scheme");
            await context.Response.WriteAsJsonAsync(new HttpValidationProblemDetails
            {
                Title = "Unsupported Scheme",
                Status = context.Response.StatusCode,
                Detail = "The Specified Scheme is not supported."
            });
            return;
        }

        var options = context.RequestServices.GetRequiredService<OAuthLibraryOptions>();
        if(string.IsNullOrEmpty(options.CallbackScheme))
        {
            context.Response.StatusCode = 204;
            context.Response.Headers.Add("Status", "No Callback Scheme is configured");
            await context.Response.WriteAsJsonAsync(new HttpValidationProblemDetails
            {
                Title = "No Callback Scheme is configured",
                Status = 204,
                Detail = "The web application has not been configured with a proper callback scheme. Please check your app's configuration.",
            });
            return;
        }

        var provider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await provider.GetAllSchemesAsync();
        if(schemes is null || !schemes.Any(x => x.Name != CookieAuthenticationDefaults.AuthenticationScheme))
        {
            context.Response.StatusCode = 204;
            context.Response.Headers.Add("Status", "No Authentication Schemes are configured");
            await context.Response.WriteAsJsonAsync(new HttpValidationProblemDetails
            {
                Title = "No Authentication Schemes Available",
                Status = 204,
                Detail = "The web application has not been configured with any Authentication Providers. Please check your app's configuration.",
            });
            return;
        }

        var authenticationScheme = schemes.FirstOrDefault(x => x.Name.Equals(scheme, StringComparison.InvariantCultureIgnoreCase));

        if(authenticationScheme is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new HttpValidationProblemDetails
            {
                Title = "Authentication Scheme not found",
                Status = 404,
                Detail = $"The web application has not been configured with the scheme '{scheme}'.",
            });
            return;
        }

        var auth = await context.AuthenticateAsync(authenticationScheme.Name);

        if (!auth.Succeeded
            || auth?.Principal == null
            || !auth.Principal.Identities.Any(id => id.IsAuthenticated)
            || string.IsNullOrEmpty(auth.Properties.GetTokenValue("access_token")))
        {
            // Not authenticated, challenge
            await context.ChallengeAsync(authenticationScheme.Name);
            return;
        }

        var handler = context.RequestServices.GetRequiredService<IMobileAuthClaimsHandler>();
        var claims = await handler.GenerateClaims(context, auth, authenticationScheme.Name);
        if(!claims.Any())
        {
            context.Response.StatusCode = 401;
            return;
        }

        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
        var outputClaims = new Dictionary<string, string>
        {
            { "access_token", tokenService.BuildToken(claims) },
            { "id_token", claims.ContainsKey("id_token") ? claims["id_token"] : string.Empty },
            { "expires_in", claims["expires_in"] }
        };

        // Build the result url
        var url = GetRedirectUri(options.CallbackScheme, outputClaims);

        // Redirect to final url
        context.Response.Redirect(url);
    }

    private static string GetRedirectUri(string callbackScheme, Dictionary<string, string> claims)
    {
        var qs = claims.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value != "-1")
            .Select(kvp => $"{kvp.Key}={kvp.Value}");
        return $"{callbackScheme}://auth?{string.Join("&", qs)}";
    }
}
