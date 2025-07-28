using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace BellwoodGlobal.Mobile
{
    [Activity(NoHistory = true,
              Exported = true,
              LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
      new[] { Intent.ActionView },
      Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
      DataScheme = "com.bellwoodglobal.mobile",
      DataHost = "callback"
    )]
    public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
    {
        // no overrides needed here
    }
}
