using AvantiPoint.MobileAuth.Authentication;
using AvantiPoint.MobileAuth.Stores;
using FileContextCore;
using FileContextCore.FileManager;
using FileContextCore.Serializer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace AvantiPoint.MobileAuth;

public class MobileAuthenticationBuilder : AuthenticationBuilder
{
    public MobileAuthenticationBuilder(AuthenticationBuilder builder) 
        : base(builder.Services)
    {
    }

    public MobileAuthenticationBuilder AddMobileAuthClaimsHandler<T>()
        where T : class, IMobileAuthClaimsHandler
    {
        Services.AddScoped<IMobileAuthClaimsHandler, T>();
        return this;
    }

    public MobileAuthenticationBuilder AddTokenService<T>()
        where T : class, ITokenService
    {
        Services.AddScoped<ITokenService, T>();
        return this;
    }

    private bool tokenStoreConfigured;
    public MobileAuthenticationBuilder ConfigureTokenStore<TStore>()
        where TStore : class, ITokenStore
    {
        tokenStoreConfigured = true;
        Services.AddScoped<ITokenStore, TStore>();
        return this;
    }

    public MobileAuthenticationBuilder ConfigureDbTokenStore<TStore>(Action<DbContextOptionsBuilder>? optionsAction = null)
        where TStore : DbContext, ITokenStore
    {
        tokenStoreConfigured = true;
        Services.AddDbContext<TStore>(optionsAction)
            .AddScoped<ITokenStore>(sp => sp.GetRequiredService<TStore>());
        return this;
    }

    public MobileAuthenticationBuilder ConfigureDbTokenStore<TStore>(Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
        where TStore : DbContext, ITokenStore
    {
        tokenStoreConfigured = true;
        Services.AddDbContext<TStore>(optionsAction)
            .AddScoped<ITokenStore>(sp => sp.GetRequiredService<TStore>());
        return this;
    }

    internal void ConfigureDefaultServices()
    {
        Services.TryAddScoped<IMobileAuthClaimsHandler, MobileAuthClaimsHandler>();
        Services.TryAddScoped<ITokenService, TokenService>();
        if (!tokenStoreConfigured)
            ConfigureDbTokenStore<TokenStore>((services, options) =>
            {
                options.UseFileContextDatabase<JSONSerializer, DefaultFileManager>(
                    databaseName: "mobileauth", 
                    location: Path.Join("App_Data", "auth"));
            });
    }
}
