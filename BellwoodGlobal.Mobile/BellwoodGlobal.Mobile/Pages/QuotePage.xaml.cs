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
        
        // Flight picker items + handler
        FlightInfoPicker.Items.Add("None");
        FlightInfoPicker.Items.Add("Commercial Flight");
        FlightInfoPicker.Items.Add("Private Tail Number");
        FlightInfoPicker.SelectedIndexChanged += OnFlightInfoChanged;
        FlightInfoPicker.SelectedIndex = 0; // default None

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

        // Keep return in sync with pickup and validate changes
        PickupDate.DateSelected += (_, __) => SyncReturnMinAndSuggest();
        PickupTime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) SyncReturnMinAndSuggest(); };

        ReturnDatePicker.DateSelected += (_, __) => EnsureReturnAfterPickup();
        ReturnTimePicker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) EnsureReturnAfterPickup(); };
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

        if (isAsDirected) RoundTripCheck.IsChecked = false;

        ReturnSection.IsVisible = !isAsDirected && RoundTripCheck.IsChecked;
        if (ReturnSection.IsVisible) SyncReturnMinAndSuggest();
    }

    private void OnRoundTripChanged(object? sender, CheckedChangedEventArgs e)
    {
        var isAsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected;
        ReturnSection.IsVisible = !isAsDirected && e.Value;

        if (ReturnSection.IsVisible)
        {
            // Default return to the pickup’s date/time; user can adjust (Fri → Sun, etc.)
            ReturnDatePicker.Date = PickupDate.Date;
            ReturnTimePicker.Time = PickupTime.Time;
            SyncReturnMinAndSuggest();
        }
    }

    private void SyncReturnMinAndSuggest()
    {
        // Enforce that return date can't be before pickup date
        ReturnDatePicker.MinimumDate = PickupDate.Date;

        if (ReturnSection.IsVisible)
        {
            if (ReturnDatePicker.Date < ReturnDatePicker.MinimumDate)
                ReturnDatePicker.Date = PickupDate.Date;

            EnsureReturnAfterPickup(suggestIfInvalid: true);
        }
    }

    private void EnsureReturnAfterPickup(bool suggestIfInvalid = false)
    {
        var pickup = PickupDate.Date + PickupTime.Time;
        var ret = ReturnDatePicker.Date + ReturnTimePicker.Time;

        if (ret <= pickup && suggestIfInvalid)
        {
            // Suggest a valid default; change to 24h if you prefer
            var suggested = pickup.AddHours(2);
            ReturnDatePicker.Date = suggested.Date;
            ReturnTimePicker.Time = suggested.TimeOfDay;
        }
    }

    private void OnFlightInfoChanged(object? sender, EventArgs e)
    {
        var sel = FlightInfoPicker.SelectedItem?.ToString();
        if (sel == "Commercial Flight")
        {
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Flight number";
            FlightInfoEntry.Placeholder = "e.g., AA1234";
        }
        else if (sel == "Private Tail Number")
        {
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Tail number";
            FlightInfoEntry.Placeholder = "e.g., N123AB";
        }
        else
        {
            FlightInfoGrid.IsVisible = false;
            FlightInfoEntry.Text = string.Empty;
        }
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

        var pickupDT = PickupDate.Date + PickupTime.Time;
        var isAsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected;

        DateTime? retDT = null;
        if (!isAsDirected && RoundTripGrid.IsVisible && RoundTripCheck.IsChecked)
        {
            retDT = ReturnDatePicker.Date + ReturnTimePicker.Time;
            if (retDT <= pickupDT)
            {
                await DisplayAlert("Return time", "Return must be after pickup. Please adjust the date/time.", "OK");
                return;
            }
        }

        // Resolve locations
        var pickupLoc = ResolveLocation(PickupLocationPicker, PickupNewLabel, PickupNewAddress);
        string? dropLoc = isAsDirected ? null : ResolveLocation(DropoffPicker, DropoffNewLabel, DropoffNewAddress);

        // Flight info (optional)
        string? flightType = null, flightNumber = null, tailNumber = null;
        var fi = FlightInfoPicker.SelectedItem?.ToString();
        if (fi == "Commercial Flight")
        {
            flightType = "Commercial";
            flightNumber = (FlightInfoEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(flightNumber)) flightNumber = null;
        }
        else if (fi == "Private Tail Number")
        {
            flightType = "Private";
            tailNumber = (FlightInfoEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(tailNumber)) tailNumber = null;
        }

        // Outbound (first reservation)
        var outbound = new QuoteDraft
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
            PickupDateTime = pickupDT,
            PickupLocation = pickupLoc,
            AsDirected = isAsDirected,
            Hours = isAsDirected ? (int?)Math.Max(1, (int)HoursStepper.Value) : null,
            DropoffLocation = dropLoc,
            RoundTrip = !isAsDirected && RoundTripCheck.IsChecked,
            ReturnPickupTime = retDT, //  now set when round trip
            AdditionalRequest = RequestsPicker.SelectedItem?.ToString(),
            AdditionalRequestOtherText = RequestOtherGrid.IsVisible ? (RequestOtherEntry.Text ?? "") : null,

            // Flight info on the outbound leg
            FlightType = flightType,
            FlightNumber = flightNumber,
            TailNumber = tailNumber
        };

        var requests = new List<QuoteDraft> { outbound };

        // Return leg (second reservation) if round trip (not As Directed)
        if (!isAsDirected && RoundTripGrid.IsVisible && RoundTripCheck.IsChecked && retDT is not null)
        {
            var returnLeg = new QuoteDraft
            {
                Booker = outbound.Booker,
                Passenger = outbound.Passenger,
                AdditionalPassengers = outbound.AdditionalPassengers.ToList(),
                VehicleClass = outbound.VehicleClass,

                PickupDateTime = retDT.Value,
                PickupLocation = outbound.DropoffLocation ?? outbound.PickupLocation,
                AsDirected = false,
                Hours = null,
                DropoffLocation = outbound.PickupLocation,

                RoundTrip = false,
                ReturnPickupTime = null,

                AdditionalRequest = outbound.AdditionalRequest,
                AdditionalRequestOtherText = outbound.AdditionalRequestOtherText,

                // Mirror flight fields (you can omit if you prefer them only on inbound)
                // TODO add option to change in case of different flight info
                FlightType = flightType,
                FlightNumber = flightNumber,
                TailNumber = tailNumber
            };

            requests.Add(returnLeg);
        }

        var wrapper = new { requestCount = requests.Count, requests };
        var json = System.Text.Json.JsonSerializer.Serialize(wrapper,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

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
