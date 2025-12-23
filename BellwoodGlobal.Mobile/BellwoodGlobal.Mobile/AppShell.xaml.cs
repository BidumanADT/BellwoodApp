using Microsoft.Maui.Controls;

namespace BellwoodGlobal.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(Pages.QuotePage), typeof(Pages.QuotePage));
        Routing.RegisterRoute(nameof(Pages.RideHistoryPage), typeof(Pages.RideHistoryPage));
        Routing.RegisterRoute(nameof(Pages.QuoteDashboardPage), typeof(Pages.QuoteDashboardPage));
        Routing.RegisterRoute(nameof(Pages.QuoteDetailPage), typeof(Pages.QuoteDetailPage));
        Routing.RegisterRoute(nameof(Pages.BookRidePage), typeof(Pages.BookRidePage));     
        Routing.RegisterRoute(nameof(Pages.BookingsPage), typeof(Pages.BookingsPage));
        Routing.RegisterRoute(nameof(Pages.BookingDetailPage), typeof(Pages.BookingDetailPage));
        Routing.RegisterRoute(nameof(Pages.DriverTrackingPage), typeof(Pages.DriverTrackingPage));
    }
}
