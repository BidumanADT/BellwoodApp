namespace BellwoodGlobal.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Use AppShell always, then control nav via routes
        MainPage = new AppShell();

        // Decide initial route
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            // Force Shell to navigate to LoginPage first
            Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
