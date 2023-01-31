using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AvantiPoint.MobileAuth.Authentication;
using AvantiPoint.MobileAuth.Configuration;
using AvantiPoint.MobileAuth.Http;
using AvantiPoint.MobileAuth.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AvantiPoint.MobileAuth;

public static class MobileAuth
{
    private const string DefaultUserProfileRoute = "/api/users/me";
    private const string Tag = nameof(MobileAuth);

    public static WebApplicationBuilder AddMobileAuth(this WebApplicationBuilder appBuilder) =>
        appBuilder.AddMobileAuth(_ => { });

    public static WebApplicationBuilder AddMobileAuth(this WebApplicationBuilder appBuilder, Action<MobileAuthenticationBuilder> configureAuthenticationBuilder)
    {
        var options = appBuilder.Configuration
            .GetSection("OAuth")
            .Get<OAuthLibraryOptions>() ?? new OAuthLibraryOptions();

        if(string.IsNullOrEmpty(options.AuthPath))
        {
            options.AuthPath = "mobileauth";
        }

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
                o.LoginPath = options.Signin;
                o.LogoutPath = options.Signout;
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
            .AddSingleton<ITokenOptions>(sp => sp.GetRequiredService<OAuthLibraryOptions>())
            .AddHttpContextAccessor();

        var mobileAuthBuilder = new MobileAuthenticationBuilder(authBuilder);
        configureAuthenticationBuilder(mobileAuthBuilder);
        mobileAuthBuilder.ConfigureDefaultServices();

        return appBuilder;
    }

    public static WebApplication MapDefaultMobileAuthRoutes(this WebApplication app) =>
        app.MapMobileAuthRoute()
           .MapMobileAuthLogoutRoute()
           .MapMobileAuthUserClaimsRoute(DefaultUserProfileRoute);

    private static string GetPath(PathString path, string defaultValue) =>
        path.HasValue ? path.Value : defaultValue;

    public static WebApplication MapMobileAuthRoute(this WebApplication app, string name = "signin")
    {
        var options = app.Services.GetRequiredService<OAuthLibraryOptions>();

        app.MapGet($"{options.Signin}{{scheme}}", Signin)
           .Produces(302)
           .ProducesProblem(204)
           .ProducesProblem(404)
           .WithTags(Tag)
#if NET7_0_OR_GREATER
           .WithSummary("OAuth Login Endpoint.")
           .WithDescription("This will redirect to the appropriate OAuth provider such as Apple, Google or Microsoft based on the configured OAuth providers for the API.")
           .WithOpenApi()
#endif
           .AllowAnonymous()
           .WithName(name)
           .WithDisplayName(name);

        return app;
    }

    public static WebApplication MapMobileAuthLogoutRoute(this WebApplication app, string name = "signout")
    {
        var options = app.Services.GetRequiredService<OAuthLibraryOptions>();

        app.MapGet(options.Signout, Signout)
            .WithTags(Tag)
#if NET7_0_OR_GREATER
            .WithSummary("Revokes user Token.")
            .WithDescription("This will revoke the user's token effectively logging out the user.")
            .WithOpenApi()
#endif
            .WithName(name)
            .RequireAuthorization();
        return app;
    }

    public static WebApplication MapMobileAuthUserClaimsRoute(this WebApplication app, string routeTemplate, string name = "user-profile")
    {
        app.MapGet(routeTemplate, GetProfile)
           .WithTags(Tag)
#if NET7_0_OR_GREATER
           .WithSummary("Provides dictionary of user claims.")
           .WithDescription("This will return the user claims for the currently authenticated user.")
           .WithOpenApi()
#endif
           .RequireAuthorization()
           .WithName(name);
        return app;
    }

    private static Task GetProfile(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 200;
        var claims = context.User.Claims.ToDictionary(x => GetKey(x), x => x.Value);
        return context.Response.WriteAsJsonAsync(claims);
    }

    private static string GetKey(Claim claim) =>
        claim.Properties.Any() ? claim.Properties.First().Value : claim.Type;

    private static async Task Signout(HttpContext context, CancellationToken cancellationToken)
    {
        var provider = context.User.FindFirstValue("provider");
        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
        string? authHeader = context.Request.Headers.Authorization;
        if(!string.IsNullOrEmpty(authHeader))
        {
            var token = Regex.Replace(authHeader, "Bearer", string.Empty).Trim();
            if (!string.IsNullOrEmpty(token))
            {
                await tokenService.InvalidateToken(token);
                await context.Ok();
            }
        }

        await context.BadRequest();
    }

    private static async Task Signin(string scheme, ILoggerFactory loggerFactory, HttpContext context)
    {
        var logger = loggerFactory.CreateLogger(nameof(MobileAuth));
        if(scheme.Equals(CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.InvariantCultureIgnoreCase) ||
            scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogError($"'{scheme}' is an unsupported login provider.");
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
            logger.LogError($"No Callback Scheme is configured for {scheme}.");
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

        var ignoreSchemes = new[]
        {
            CookieAuthenticationDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme
        };

        var provider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await provider.GetAllSchemesAsync();
        if(schemes is null || !schemes.Any(x => !ignoreSchemes.Contains(x.Name)))
        {
            logger.LogError($"No authentication schemes defined.");
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
            logger.LogError($"No authentication scheme found matching {scheme}.");
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
            { "access_token", await tokenService.BuildToken(claims) },
            { "id_token", claims.FindFirstValue("id_token") ?? string.Empty },
            { "expires_in", claims.FindFirstValue("expires_in") ?? string.Empty }
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
