using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookingsPage : ContentPage
{
    private readonly IRideService _rides;

    public BookingsPage(IRideService rides)
    {
        InitializeComponent();
        _rides = rides;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var items = await _rides.ListAsync();
            List.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load bookings.\n{ex.Message}", "OK");
        }
    }
}
