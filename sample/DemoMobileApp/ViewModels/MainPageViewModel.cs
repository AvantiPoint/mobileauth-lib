using System.Collections.ObjectModel;
using DemoMobileApp.Services;
using MauiMicroMvvm;
using Microsoft.Extensions.Logging;

namespace DemoMobileApp.ViewModels;

public class MainPageViewModel : MauiMicroViewModel
{
    private ISecureStorage _storage { get; }
    private IWebAuthenticator _webAuthenticator { get; }
    private IUserProfileService _userProfile { get; }

    public MainPageViewModel(ViewModelContext context, ISecureStorage storage, IWebAuthenticator webAuthenticator, IUserProfileService userProfile)
        : base(context)
    {
        _storage = storage;
        _webAuthenticator = webAuthenticator;
        _userProfile = userProfile;
        LoginCommand = new (OnLoginCommandExecuted);
        Claims = new();
    }

    public ObservableCollection<string> Claims { get; }

    public Command<string> LoginCommand { get; }

    private async void OnLoginCommandExecuted(string scheme)
    {
        try
        {
            var result = await _webAuthenticator.AuthenticateAsync(new WebAuthenticatorOptions
            {
                CallbackUrl = new Uri($"{Constants.CallbackScheme}://"),
                Url = new Uri(Constants.BaseUrl)
            });

            await _storage.SetAsync("access_token", result.AccessToken);

            using var response = await _userProfile.GetProfileClaims();
            if(response.IsSuccessStatusCode)
            {
                Claims.Clear();
                var claims = response.Content.Select(x => $"{x.Key}: {x.Value}");
                foreach (var claim in claims)
                    Claims.Add(claim);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error occurred.");
        }
    }
}
