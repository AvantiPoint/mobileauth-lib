using CommunityToolkit.Maui;
using DemoMobileApp.Services;
using DemoMobileApp.ViewModels;
using Refit;

namespace DemoMobileApp;

public static class Constants
{
    public const string BaseUrl = "https://localhost:7172";
    public const string CallbackScheme = "myapp-scheme";
}

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiMicroMvvm<AppShell>(
                "Resources/Styles/Colors.xaml", 
                "Resources/Styles/Styles.xaml")
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(WebAuthenticator.Default)
            .AddSingleton(AppleSignInAuthenticator.Default)
            .AddSingleton(SecureStorage.Default)
            .AddRefitClient<IUserProfileService>()
            .AddTransient<MainPage>()
            .AddTransient<MainPageViewModel>();

        return builder.Build();
    }

    public static IServiceCollection AddRefitClient<T>(this IServiceCollection services)
        where T : class
    {
        services.AddSingleton(sp =>
        {
            var settings = new RefitSettings
            {
                AuthorizationHeaderValueGetter = () => sp.GetRequiredService<ISecureStorage>().GetAsync("access_token")
            };
            return RestService.For<T>(Constants.BaseUrl, settings);
        });
        return services;
    }
}