using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace BellwoodGlobal.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Always start with Shell (routes defined in AppShell.xaml)
        MainPage = new AppShell();

        // Defer the secure token check until the UI thread is ready
        Dispatcher.Dispatch(async () => await EnsureSignedInAsync());
    }

    /// Checks for an access token in SecureStorage and routes to Login if missing.
    private static async Task EnsureSignedInAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("access_token");

            // No token? Send the user to the login route.
            if (string.IsNullOrEmpty(token))
            {
                // Use absolute route so we always land on LoginPage
                await Shell.Current.GoToAsync("//LoginPage");
            }
            // else: stay on whatever your Shell’s default content is (e.g., MainPage)
        }
        catch
        {
            // SecureStorage can throw on some desktop simulators/emulators or if keychain/keystore is unavailable.
            // Fall back to forcing a fresh sign-in.
            await Shell.Current.DisplayAlert("Sign-in required",
                "Secure storage is unavailable. Please sign in again.", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
