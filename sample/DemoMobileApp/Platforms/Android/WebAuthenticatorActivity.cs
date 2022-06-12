using Android.App;
using Android.Content;
using Android.Content.PM;

namespace DemoMobileApp;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataScheme = Constants.CallbackScheme)]
public class WebAuthenticatorActivity : WebAuthenticatorCallbackActivity
{
}