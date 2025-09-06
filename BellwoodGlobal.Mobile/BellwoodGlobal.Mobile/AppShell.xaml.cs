using Microsoft.Maui.Controls;

namespace BellwoodGlobal.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register non-visual routes
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }
}
