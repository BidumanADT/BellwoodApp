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
        string? token = null;
        try
        {
            token = await SecureStorage.GetAsync("access_token");
        }
        catch
        {
            // Don’t block on an alert here; just route to login
            token = null;
        }

        if (string.IsNullOrEmpty(token))
        {
            // Registered route (relative), not absolute:
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
        // else: stay on MainPage
    }
}
