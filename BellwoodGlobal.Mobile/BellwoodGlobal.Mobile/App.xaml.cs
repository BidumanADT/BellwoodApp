using BellwoodGlobal.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // If token exists, go straight to MainPage, else LoginPage
        if (Preferences.ContainsKey("access_token"))
            MainPage = new AppShell();
        else
            MainPage = new LoginPage(
                ServiceHelper.GetService<IHttpClientFactory>());
    }
}
