using Microsoft.Maui.Controls;

namespace BellwoodGlobal.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        //InitializeComponent();

        //// Register the other pages as routes (not visible as tabs)
        //Routing.RegisterRoute("QuotePage", typeof(Pages.QuotePage));
        //Routing.RegisterRoute("RideHistoryPage", typeof(Pages.RideHistoryPage));
        //Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        //Routing.RegisterRoute("QuoteDashboardPage", typeof(Pages.QuoteDashboardPage));
        //Routing.RegisterRoute("QuoteDetailPage", typeof(Pages.QuoteDetailPage));

        InitializeComponent();
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(Pages.QuotePage), typeof(Pages.QuotePage));
        Routing.RegisterRoute(nameof(Pages.RideHistoryPage), typeof(Pages.RideHistoryPage));
        Routing.RegisterRoute(nameof(Pages.QuoteDashboardPage), typeof(Pages.QuoteDashboardPage));
        Routing.RegisterRoute(nameof(Pages.QuoteDetailPage), typeof(Pages.QuoteDetailPage));
    }
}
