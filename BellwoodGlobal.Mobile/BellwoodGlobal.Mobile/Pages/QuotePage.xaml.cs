using System.Collections.ObjectModel;
using System.Text.Json;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;

namespace BellwoodGlobal.Mobile.Pages;

public partial class QuotePage : ContentPage
{
    private readonly IProfileService _profile;
    private readonly ObservableCollection<string> _additionalPassengers = new();

    private const string PassengerSelf = "Booker (you)";
    private const string PassengerNew = "New Passenger";
    private const string LocationNew = "New Location";
    private const string AsDirected = "As Directed";

    private List<Passenger> _savedPassengers = new();
    private List<Models.Location> _savedLocations = new();

    public QuotePage()
    {
        InitializeComponent();
        _profile = ServiceHelper.GetRequiredService<IProfileService>();

        // Booker
        var booker = _profile.GetBooker();
        BookerFirst.Text = booker.FirstName;
        BookerLast.Text = booker.LastName;
        BookerPhone.Text = booker.PhoneNumber;
        BookerEmail.Text = booker.EmailAddress;

        // Data
        _savedPassengers = _profile.GetSavedPassengers().ToList();
        _savedLocations = _profile.GetSavedLocations().ToList();

        // Passenger picker
        PassengerPicker.Items.Add(PassengerSelf);
        foreach (var p in _savedPassengers) PassengerPicker.Items.Add(p.ToString());
        PassengerPicker.Items.Add(PassengerNew);
        PassengerPicker.SelectedIndexChanged += OnPassengerChanged;

        // Vehicle
        foreach (var v in new[] { "Sedan", "SUV", "Sprinter", "S-Class" }) VehiclePicker.Items.Add(v);
        VehiclePicker.SelectedIndex = 0;

        // Pickup
        foreach (var loc in _savedLocations) PickupLocationPicker.Items.Add(loc.ToString());
        PickupLocationPicker.Items.Add(LocationNew);
        PickupLocationPicker.SelectedIndexChanged += OnPickupLocationChanged;

        // Dropoff
        DropoffPicker.Items.Add(AsDirected);
        foreach (var loc in _savedLocations) DropoffPicker.Items.Add(loc.ToString());
        DropoffPicker.Items.Add(LocationNew);
        DropoffPicker.SelectedIndexChanged += OnDropoffChanged;

        // Requests
        foreach (var r in new[] { "Child Seats", "Accessible Vehicle", "Other" }) RequestsPicker.Items.Add(r);
        RequestsPicker.SelectedIndexChanged += (_, __) =>
            RequestOtherGrid.IsVisible = RequestsPicker.SelectedItem?.ToString() == "Other";

        AdditionalPassengersList.ItemsSource = _additionalPassengers;

        var now = DateTime.Now.AddMinutes(30);
        PickupDate.Date = now.Date;
        PickupTime.Time = now.TimeOfDay;

        // default return = same day, +2h (easy to change by user)
        ReturnDatePicker.Date = PickupDate.Date;
        ReturnTimePicker.Time = PickupTime.Time.Add(TimeSpan.FromHours(2));
    }

    private void OnPassengerChanged(object? sender, EventArgs e)
    {
        var sel = PassengerPicker.SelectedItem?.ToString();
        PassengerNewGrid.IsVisible = sel == PassengerNew;

        if (sel == PassengerSelf)
        {
            PassengerFirst.Text = BookerFirst.Text;
            PassengerLast.Text = BookerLast.Text;
            PassengerPhone.Text = BookerPhone.Text;
            PassengerEmail.Text = BookerEmail.Text;
            PassengerNewGrid.IsVisible = true;
        }
        else if (sel != PassengerNew && !string.IsNullOrEmpty(sel))
        {
            var p = _savedPassengers.FirstOrDefault(x => x.ToString() == sel);
            if (p != null)
            {
                PassengerFirst.Text = p.FirstName;
                PassengerLast.Text = p.LastName;
                PassengerPhone.Text = p.PhoneNumber;
                PassengerEmail.Text = p.EmailAddress;
                PassengerNewGrid.IsVisible = true;
            }
        }
    }

    private void OnPickupLocationChanged(object? sender, EventArgs e)
        => PickupNewGrid.IsVisible = PickupLocationPicker.SelectedItem?.ToString() == LocationNew;

    private void OnDropoffChanged(object? sender, EventArgs e)
    {
        var sel = DropoffPicker.SelectedItem?.ToString();
        var isAsDirected = sel == AsDirected;

        AsDirectedHoursGrid.IsVisible = isAsDirected;
        RoundTripGrid.IsVisible = !isAsDirected;
        DropoffNewGrid.IsVisible = sel == LocationNew;

        // keep return time in sync with current checkbox state
        ReturnWhenGrid.IsVisible = !isAsDirected && RoundTripCheck.IsChecked;
    }


    private void OnRoundTripChanged(object? sender, CheckedChangedEventArgs e)
    {
        var isAsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected;
        ReturnWhenGrid.IsVisible = !isAsDirected && e.Value;
    }


    private void OnAddAdditionalPassenger(object? sender, EventArgs e)
    {
        var name = (AdditionalPassengerEntry.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(name))
        {
            _additionalPassengers.Add(name);
            AdditionalPassengerEntry.Text = "";
        }
    }

    private void OnRemoveAdditionalPassenger(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is string name)
            _additionalPassengers.Remove(name);
    }

    private async void OnBuildJson(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BookerPhone.Text) || string.IsNullOrWhiteSpace(BookerEmail.Text))
        {
            await DisplayAlert("Required", "Booker phone and email are required.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(PassengerFirst.Text) || string.IsNullOrWhiteSpace(PassengerLast.Text))
        {
            await DisplayAlert("Required", "Passenger first and last name are required.", "OK");
            return;
        }

        var pickupDateTime = PickupDate.Date + PickupTime.Time;

        DateTime? returnDateTime = null;
        if (RoundTripGrid.IsVisible && RoundTripCheck.IsChecked)
        {
            returnDateTime = ReturnDatePicker.Date + ReturnTimePicker.Time;

            if (returnDateTime <= pickupDateTime)
            {
                await DisplayAlert("Return must be later",
                    "Please choose a return date/time after the pickup.", "OK");
                return;
            }
        }

        var draft = new QuoteDraft
        {
            Booker = new Passenger
            {
                FirstName = BookerFirst.Text ?? "",
                LastName = BookerLast.Text ?? "",
                PhoneNumber = BookerPhone.Text,
                EmailAddress = BookerEmail.Text
            },
            Passenger = new Passenger
            {
                FirstName = PassengerFirst.Text ?? "",
                LastName = PassengerLast.Text ?? "",
                PhoneNumber = string.IsNullOrWhiteSpace(PassengerPhone.Text) ? BookerPhone.Text : PassengerPhone.Text,
                EmailAddress = string.IsNullOrWhiteSpace(PassengerEmail.Text) ? BookerEmail.Text : PassengerEmail.Text
            },
            AdditionalPassengers = _additionalPassengers.ToList(),
            VehicleClass = VehiclePicker.SelectedItem?.ToString() ?? "Sedan",
            PickupDateTime = pickupDateTime,
            PickupLocation = ResolveLocation(PickupLocationPicker, PickupNewLabel, PickupNewAddress),
            AsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected,
            Hours = (DropoffPicker.SelectedItem?.ToString() == AsDirected) ? (int?)Math.Max(1, (int)HoursStepper.Value) : null,
            DropoffLocation = DropoffPicker.SelectedItem?.ToString() == AsDirected ? null
                              : ResolveLocation(DropoffPicker, DropoffNewLabel, DropoffNewAddress),
            RoundTrip = RoundTripGrid.IsVisible && RoundTripCheck.IsChecked,
            ReturnPickupTime = returnDateTime,
            AdditionalRequest = RequestsPicker.SelectedItem?.ToString(),
            AdditionalRequestOtherText = RequestOtherGrid.IsVisible ? (RequestOtherEntry.Text ?? "") : null
        };

        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions { WriteIndented = true });
        JsonEditor.Text = json;
        JsonFrame.IsVisible = true;
        await DisplayAlert("Quote Ready", "The JSON has been built below.", "OK");
    }

    private static string ResolveLocation(Picker picker, Entry label, Entry address)
    {
        var sel = picker.SelectedItem?.ToString();
        if (sel == LocationNew)
            return $"{(label.Text ?? "").Trim()} - {(address.Text ?? "").Trim()}".Trim(' ', '-');
        return sel ?? "";
    }

    private async void OnCopyJson(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(JsonEditor.Text ?? "");
        await DisplayAlert("Copied", "Quote JSON copied to clipboard.", "OK");
    }
}
