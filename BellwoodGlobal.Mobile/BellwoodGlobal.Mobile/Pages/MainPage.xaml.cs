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

    private async void OnRideHistory(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(RideHistoryPage));  // relative push

    private async void OnLogoutClicked(object sender, EventArgs e)
        => await _auth.LogoutAsync(); // clears tokens + routes to //LoginPage per your impl
}
