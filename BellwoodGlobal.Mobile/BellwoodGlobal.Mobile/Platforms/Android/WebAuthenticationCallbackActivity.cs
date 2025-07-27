using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace BellwoodGlobal.Mobile
{
    [Activity(NoHistory = true,
              Exported = true,
              LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] {
                    Intent.CategoryDefault,
                    Intent.CategoryBrowsable
                  },
                  DataScheme = "com.bellwood.mobile",
                  DataHost = "callback")]
    public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
    {
        // nothing else needed here—base class handles the redirect for you
    }
}
