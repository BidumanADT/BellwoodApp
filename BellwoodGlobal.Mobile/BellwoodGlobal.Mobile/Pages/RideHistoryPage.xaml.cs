using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class RideHistoryPage : ContentPage
{
    private readonly IRideService _rides;

    public RideHistoryPage()
    {
        InitializeComponent();
        _rides = ServiceHelper.GetRequiredService<IRideService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Busy.IsVisible = Busy.IsRunning = true;
        try
        {
            var data = await _rides.GetHistoryAsync();
            HistoryList.ItemsSource = data;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load history.\n{ex.Message}", "OK");
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }
}
