using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class MainPage : ContentPage
{
    private readonly IAuthService _auth;

    public MainPage()
    {
        InitializeComponent();
        _auth = ServiceHelper.GetRequiredService<IAuthService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _auth.RequireSignInAsync();
        if (!await _auth.IsSignedInAsync()) return; // likely routed to LoginPage
    }

    private async void OnGetQuote(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(QuotePage));        // relative push

    private async void OnQuotesMenuClicked(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheet(
            "Quotes", "Cancel", null,
            "New Quote",
            "Quote Dashboard" // (your inbox/history page)
        );

        switch (choice)
        {
            case "New Quote":
                await Shell.Current.GoToAsync(nameof(QuotePage));
                break;
            case "Quote Dashboard":
                await Shell.Current.GoToAsync(nameof(QuoteDashboardPage));
                break;
        }
    }

    private async void OnBookingsMenuClicked(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheet(
            "Bookings", "Cancel", null,
            "My Bookings"
        );

        switch (choice)
        {
            case "My Bookings":
                await Shell.Current.GoToAsync(nameof(Pages.BookingsPage));
                break;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Log Out", "Cancel");
        if (confirmed) await _auth.LogoutAsync();
    }

    private async void OnBookRideClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Pages.BookRidePage));
    }
}
