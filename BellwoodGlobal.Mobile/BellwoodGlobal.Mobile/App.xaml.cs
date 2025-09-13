using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile;

public partial class App : Application
{
    private readonly IAuthService _auth;

    public App(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;

        MainPage = new AppShell();

        Dispatcher.Dispatch(async () =>
        {
            if (!await _auth.IsSignedInAsync())
                await Shell.Current.GoToAsync(nameof(LoginPage));
        });
    }
}
