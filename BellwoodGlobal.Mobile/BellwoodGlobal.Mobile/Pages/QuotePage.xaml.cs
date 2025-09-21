using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class QuotePage : ContentPage
{
    private readonly IQuoteService _quotes;

    public QuotePage()
    {
        InitializeComponent();
        _quotes = ServiceHelper.GetRequiredService<IQuoteService>();

        var now = DateTime.Now;
        DatePick.Date = now.Date;
        TimePick.Time = now.TimeOfDay.Add(TimeSpan.FromMinutes(30));
    }

    private async void OnGetQuote(object sender, EventArgs e)
    {
        var pickup = PickupEntry.Text ?? "";
        var dropoff = DropoffEntry.Text ?? "";
        var when = DatePick.Date + TimePick.Time;

        if (string.IsNullOrWhiteSpace(pickup) || string.IsNullOrWhiteSpace(dropoff))
        {
            await DisplayAlert("Required", "Please enter pickup and drop-off addresses.", "OK");
            return;
        }

        var estimate = await _quotes.EstimateAsync(pickup, dropoff, when);
        ResultHeader.Text = $"Estimate for {when:MMM dd, h:mm tt}";
        ResultVehicle.Text = $"Vehicle: {estimate.VehicleClass}";
        ResultFare.Text = $"Estimated Fare: ${estimate.EstimatedFare:F2}";
        ResultEta.Text = $"Estimated ETA: ~{estimate.EstimatedEta.TotalMinutes:N0} min";
    }
}
