using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;  // for MauiAppCompatActivity

namespace BellwoodGlobal.Mobile
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTask,  // must be SingleTask for OIDC callbacks
        ConfigurationChanges = ConfigChanges.ScreenSize
                              | ConfigChanges.Orientation
                              | ConfigChanges.UiMode
                              | ConfigChanges.ScreenLayout
                              | ConfigChanges.SmallestScreenSize
                              | ConfigChanges.Density)]
    //[IntentFilter(
    //    new[] { Intent.ActionView },
    //    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    //    DataScheme = "com.bellwood.mobile",
    //    DataHost = "callback")]
    public class MainActivity : MauiAppCompatActivity
    {
        
    }
}
