using System.Globalization;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookRidePage : ContentPage
{
    private readonly IRideService _rides;

    public BookRidePage()
    {
        InitializeComponent();
        _rides = ServiceHelper.GetRequiredService<IRideService>();
    }

    async void OnSubmit(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(PickupEntry.Text) ||
            string.IsNullOrWhiteSpace(DropoffEntry.Text) ||
            string.IsNullOrWhiteSpace(WhenEntry.Text))
        {
            ErrorLabel.Text = "Please fill all fields.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!DateTime.TryParse(WhenEntry.Text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var when))
        {
            ErrorLabel.Text = "Use a recognizable date/time (e.g., 2025-11-02 17:30).";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            var id = await _rides.CreateAsync(new BookRideRequest(
                PickupEntry.Text.Trim(),
                DropoffEntry.Text.Trim(),
                when));

            await DisplayAlert("Booked", $"Your ride has been requested.\nRef: {id}", "OK");
            await Shell.Current.GoToAsync("//Bookings"); // jump to bookings tab
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Failed to book: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }
}
