# MobileAuth Library

The MobileAuth Library is a helper library to help produce an OAuth endpoint using AspNetCore Minimal APIs for your Mobile Application. This can be done in just a few lines of code. Out of the box using the library you can support Sign In with Apple, Google, and Microsoft Accounts. These require no manual configuration in code and only for the configuration values to be added to the host or `appsettings.json` file. Additional / Custom providers can easily be added as well.

```cs
var builder = WebApplication.CreateBuilder(args);

builder.AddMobileAuth();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// maps https://{host}/mobileauth/{Apple|Google|Microsoft}
app.MapMobileAuthRoute();

app.Run();
```

## Configuration

The only required part of the configuration is the CallbackScheme. This can be anything you want and will be used in the redirect url. Note the redirect url will be formatted as `{CallbackScheme}://#{claims}`. This is meant to be used with the Xamarin or Maui Essentials WebAuthenticator.

```json
{
  "OAuth": {
    "CallbackScheme": "yourappscheme",
    "Apple": {
      "ServiceId": "{Apple Service Id}",
      "TeamId": "{Your Apple Team Id}",
      "KeyId": "{Your Apple Key Id}",
    },
    "Google": {
      "ClientId": "{Google Client Id}",
      "ClientSecret": "{Your Google Client Secret}",
    },
    "Microsoft": {
      "ClientId": "{Microsoft Client Id}",
      "ClientSecret": "{Your Microsoft Client Secret}",
    }
  }
}
```

### Apple Configuration

As with any app you will need to set up a new App Id in the Apple Developer Portal. Before you get very far you can grab the Team Id out of the Developer Portal. Just beneath your name in the Developer Portal you should see the Company Name / Team Name along with the Team Id `My Company - VK8ZR2JK2E`. You'll use the `VK8ZR2JK2E` as the Team Id in your configuration.

If you have not already created an App Id, you should start there. For this example we'll say the App Id is `com.example.myapp`. Be sure to enable the `Sign In with Apple` capability. 

Once you've done this you should create a Key. Select the Keys option and then create a new Key. You can give it a name like `MyAppSIWA`, be sure to select the `Sign in with Apple` option. You'll need to click the configure button and select the Primary App Id that you created in the previous step, and hit save.

> NOTE: 
> When selecting the primary app id, it will show up like `My Awesome App (DKD783KDELD.com.example.myapp)`, where `DKD783KDELD` is the App Id. It will then show below a `Grouped App Id` like `DKD783KDELD.com.example.myapp.sid`.

Once you have the Key, it should have downloaded with a file name like `AuthKey_IUK783KD3R9.p8`, where `IUK783KD3R9` is the Key Id that you will need for your configuration.

When you're done you'll want to go back to the Identifiers and toggle from `App IDs` to `Service IDs`. You will need to create a the Service Id for your App as `com.example.myapp.sid` which you saw in the Grouped App Id, you will naturally provide this as the Service Id in your configuration. Again enable the `Sign In with Apple` capability, and this time when you configure it, it will prompt you for a host name and callback. Apple will NOT allow you to use localhost as an authorized host. You must deploy this or update your hosts file have something like `myapp.com` mapped back to `127.0.0.1`. You can then use `myapp.com` as an authorized host where the callback is `https://myapp.com/signin-apple`.

> NOTE:
> Be sure the generated key is in the `App_Data` directory with the name `AuthKey_{Your KeyId}.p8`.

To provide additional flexibility you can provide values for the following optional configuration values:

```json
{
  "OAuth": {
    "Apple": {
      "PrivateKey": "{The text value for your private key}", // Recommended for development only
      "UseAzureKeyVault": true // Optional, defaults to false
    }
  }
}
```

When using Azure Key Vault we will only update the Apple Registration to ensure that your p8 is loaded from the Azure Key Vault however you will still need to properly configure your application to [connect to the Azure Key Vault](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-6.0&WT.mc_id=DT-MVP-5002924).

### Google / Microsoft Configuration

Microsoft actually has decent docs on this please see:

- [Google](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-6.0&WT.mc_id=DT-MVP-5002924) - To get your client id and secret go to [Google API & Services](https://console.cloud.google.com/apis/credentials)
- [Microsoft](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?view=aspnetcore-6.0&WT.mc_id=DT-MVP-5002924) - To get your client id & secret [create an Application in Azure](https://go.microsoft.com/fwlink/?linkid=2083908&WT.mc_id=DT-MVP-5002924)

Again once you've got your Client Id & Client Secret you simply need to provide them in your configuration when using this library.

### Additional Providers

You can opt out of using any built in providers by simply not providing the required configuration values. In order to add additional providers you can access the AuthenticationBuilder and register any other providers you may need when calling the `AddMobileAuth` method.

```cs
builder.AddMobileAuth(auth => {
    auth.AddFacebook(o => {
        o.ClientId = "{Facebook Client Id}";
        o.ClientSecret = "{Facebook Client Secret}";
    });
    // etc...
});
```

### Customize Returned Claims

By Default the library will attempt to return the following claims:

- The User's Given Name, Surname, & Full Name
- The User's Email Address
- The Authentication Provider (Apple, Google, Microsoft)
- The Authentication Provider's User/Object Id
- The Access & Refresh Tokens
- When the Token Expires as a UTC time in Unix Seconds

Whether you need to inject some additional logic or if you just want to customize how the claims are returned, it is very easy to do. You simply need to implement `IMobileAuthClaimsHandler` and register it with the `IServiceCollection` like so:

```cs
builder.Services.AddScoped<IMobileAuthClaimsHandler, MyCustomMobileAuthClaimsHandler>();
```

## Run The Sample

Each of the supported providers has a default callback `signin-{provider}`. For example, when configuring the domain & callback in the Google console for local testing with the demo app you would use `https://localhost:7172/signin-google`. Similarly you would use the localhost domain for Microsoft. However it is important to note that Apple does NOT support localhost. In the case of Apple, for local testing you will need to use a normal formatted (does not need to be real) domain. You can then update the hosts file on your local machine to map the domain to the localhost IP address.