using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile;

public partial class App : Application
{
    public App(IAuthService auth)
    {
        InitializeComponent();
        // SplashPage handles the rest
        MainPage = new Pages.SplashPage(auth);
    }
}
