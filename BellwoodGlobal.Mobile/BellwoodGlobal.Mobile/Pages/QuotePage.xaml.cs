using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using System.Text.Json.Serialization;

namespace BellwoodGlobal.Mobile.Pages;

public partial class QuotePage : ContentPage
{
    private readonly IProfileService _profile;
    private readonly ObservableCollection<string> _additionalPassengers = new();
    private readonly IQuoteDraftBuilder _draftBuilder;
    // --- passenger/location UI constants (avoid string typos) ---
    private const string PassengerSelf = "Booker (you)";
    private const string PassengerNew = "New Passenger";
    private const string LocationNew = "New Location";
    private const string AsDirected = "As Directed";
    // --- flight UI constants (avoid string typos) ---
    private const string FlightOptionTBD = "TBD";
    private const string FlightOptionCommercial = "Commercial Flight";
    private const string FlightOptionPrivate = "Private Tail Number";
    // -----------------------------------------------
    private const string ReqMeetAndGreet = "Meet & Greet";

    private List<Passenger> _savedPassengers = new();
    private List<Models.Location> _savedLocations = new();
    // --- flight state for return logic ---
    private bool _allowReturnTailChange;     // private only
    private bool _requestsHasMeetOption;

    private static bool IsAirportText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.ToLowerInvariant();
        return t.Contains("airport") || t.Contains("fbo"); // extend as needed
    }

    public QuotePage()
    {
        InitializeComponent();
        _profile = ServiceHelper.GetRequiredService<IProfileService>();
        _draftBuilder = ServiceHelper.GetRequiredService<IQuoteDraftBuilder>();

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

        PickupStylePicker.SelectedIndexChanged += (_, __) => UpdatePickupStyleAirportUx();
        ReturnPickupStylePicker.SelectedIndexChanged += (_, __) => UpdateReturnPickupStyleAirportUx();

        // Flight picker items + handler
        FlightInfoPicker.Items.Add("TBD");
        FlightInfoPicker.Items.Add("Commercial Flight");
        FlightInfoPicker.Items.Add("Private Tail Number");
        FlightInfoPicker.SelectedIndexChanged += OnFlightInfoChanged;
        FlightInfoPicker.SelectedIndex = 0; // default TBD

        // Dropoff
        DropoffPicker.Items.Add(AsDirected);
        foreach (var loc in _savedLocations) DropoffPicker.Items.Add(loc.ToString());
        DropoffPicker.Items.Add(LocationNew);
        DropoffPicker.SelectedIndexChanged += OnDropoffChanged;

        // Requests
        foreach (var r in new[] { "Child Seats", "Accessible Vehicle", "Other" }) RequestsPicker.Items.Add(r);
        RequestsPicker.SelectedIndexChanged += (_, __) =>
        {
            var sel = RequestsPicker.SelectedItem?.ToString();
            RequestOtherGrid.IsVisible = sel == "Other";
            NonAirportMeetGrid.IsVisible = sel == ReqMeetAndGreet;
        };

        AdditionalPassengersList.ItemsSource = _additionalPassengers;

        var now = DateTime.Now.AddMinutes(30);
        PickupDate.Date = now.Date;
        PickupTime.Time = now.TimeOfDay;

        // Keep return in sync with pickup and validate changes
        PickupDate.DateSelected += (_, __) => SyncReturnMinAndSuggest();
        PickupTime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) SyncReturnMinAndSuggest(); };

        ReturnDatePicker.DateSelected += (_, __) => EnsureReturnAfterPickup();
        ReturnTimePicker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) EnsureReturnAfterPickup(); };

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
    }

    private void OnPassengerChanged(object? sender, EventArgs e)
    {
        var sel = PassengerPicker.SelectedItem?.ToString();
        PassengerNewGrid.IsVisible = sel == PassengerNew;

        if (sel == PassengerSelf)
        {
            PassengerFirst.Text = BookerFirst.Text;
            PassengerLast.Text = BookerLast.Text;
            PassengerPhone.Text = "";
            PassengerEmail.Text = "";
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

    private void OnPickupLocationChanged(object? s, EventArgs e)
    {
        PickupNewGrid.IsVisible = PickupLocationPicker.SelectedItem?.ToString() == LocationNew;
        UpdatePickupStyleAirportUx();
    }

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

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
    }

    private void OnRoundTripChanged(object? sender, CheckedChangedEventArgs e)
    {
        var isAsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected;
        ReturnSection.IsVisible = !isAsDirected && e.Value;

        if (ReturnSection.IsVisible)
        {
            // Default return to the pickup’s date/time; user can adjust
            ReturnDatePicker.Date = PickupDate.Date;
            ReturnTimePicker.Time = PickupTime.Time;
            SyncReturnMinAndSuggest();
        }

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
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
            // Suggest a valid default
            var suggested = pickup.AddHours(2);
            ReturnDatePicker.Date = suggested.Date;
            ReturnTimePicker.Time = suggested.TimeOfDay;
        }
    }

    private void OnFlightInfoChanged(object? sender, EventArgs e)
    {
        var sel = FlightInfoPicker.SelectedItem?.ToString();

        if (sel == FlightOptionCommercial)
        {
            // Outbound
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Flight number";
            FlightInfoEntry.Placeholder = "e.g., AA1234";
            // reset private-only toggle
            if (ReturnTailChangeSwitch.IsToggled) ReturnTailChangeSwitch.IsToggled = false;
            _allowReturnTailChange = false;
        }
        else if (sel == FlightOptionPrivate)
        {
            // Outbound
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Tail number";
            FlightInfoEntry.Placeholder = "e.g., N123AB";
        }
        else // TBD
        {
            FlightInfoGrid.IsVisible = false;
            FlightInfoEntry.Text = string.Empty;
            ReturnFlightEntry.Text = "To be determined";

            if (ReturnTailChangeSwitch.IsToggled) ReturnTailChangeSwitch.IsToggled = false;
            _allowReturnTailChange = false;
        }

        UpdateReturnFlightUx();
    }
    private void UpdateReturnFlightUx()
    {
        var isRoundTrip = ReturnSection.IsVisible && RoundTripCheck.IsChecked;
        var sel = FlightInfoPicker.SelectedItem?.ToString();

        var isCommercial = sel == FlightOptionCommercial;
        var isPrivate = sel == FlightOptionPrivate;

        // Show/hide the "change aircraft?" row (Private only)
        ReturnTailChangeRow.IsVisible = isRoundTrip && isPrivate;

        // Show the return entry when:
        // - Commercial (always requires its own return flight number), OR
        // - Private and the switch is ON (changing aircraft)
        if (isRoundTrip && (isCommercial || (isPrivate && _allowReturnTailChange)))
        {
            ReturnFlightGrid.IsVisible = true;
            ReturnFlightLabel.Text = isPrivate ? "Return tail number" : "Return flight number";
            if (string.IsNullOrWhiteSpace(ReturnFlightEntry.Text))
                ReturnFlightEntry.Placeholder = isPrivate ? "e.g., N987CD" : "e.g., UA4321";
        }
        else
        {
            ReturnFlightGrid.IsVisible = false;
            ReturnFlightEntry.Text = string.Empty;
        }
    }
    private void OnReturnTailChangeToggled(object? sender, ToggledEventArgs e)
    {
        _allowReturnTailChange = e.Value;
        UpdateReturnFlightUx(); // show/hide the return entry accordingly
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
    private void EnsureRequestsMeetOptionVisible(bool visible)
    {
        if (visible && !_requestsHasMeetOption)
        {
            RequestsPicker.Items.Insert(0, ReqMeetAndGreet); // stick it near the top
            _requestsHasMeetOption = true;
        }
        else if (!visible && _requestsHasMeetOption)
        {
            // clear selection if it was selected
            if (RequestsPicker.SelectedItem?.ToString() == ReqMeetAndGreet)
            {
                RequestsPicker.SelectedIndex = -1;
                NonAirportMeetGrid.IsVisible = false;
                NonAirportMeetSignEntry.Text = string.Empty;
            }
            // remove from items
            var idx = RequestsPicker.Items.IndexOf(ReqMeetAndGreet);
            if (idx >= 0) RequestsPicker.Items.RemoveAt(idx);
            _requestsHasMeetOption = false;
        }
    }
    private void UpdatePickupStyleAirportUx()
    {
        var pickupLoc = ResolveLocation(PickupLocationPicker, PickupNewLabel, PickupNewAddress);
        var isAirportPickup = IsAirportText(pickupLoc);

        // Airport → show style picker; Non-airport → hide style picker and use Additional Requests option
        PickupStyleRow.IsVisible = isAirportPickup;
        if (!isAirportPickup)
        {
            PickupSignGrid.IsVisible = false;
            PickupStylePicker.SelectedIndex = 0; // curbside by default (hidden)
        }
        EnsureRequestsMeetOptionVisible(!isAirportPickup);

        // Show/hide sign for airport Meet & Greet
        var meetSelected = PickupStylePicker.SelectedItem?.ToString() == "Meet & Greet";
        PickupSignGrid.IsVisible = isAirportPickup && meetSelected;
    }

    private void UpdateReturnPickupStyleAirportUx()
    {
        if (!ReturnSection.IsVisible)
        {
            ReturnPickupStyleRow.IsVisible = false;
            ReturnPickupSignGrid.IsVisible = false;
            return;
        }

        // Return pickup location is the current Dropoff (or new dropoff)
        var dropLoc = ResolveLocation(DropoffPicker, DropoffNewLabel, DropoffNewAddress);
        var isAirportReturnPickup = IsAirportText(dropLoc);

        ReturnPickupStyleRow.IsVisible = isAirportReturnPickup;
        if (!isAirportReturnPickup)
        {
            ReturnPickupSignGrid.IsVisible = false;
            ReturnPickupStylePicker.SelectedIndex = 0;
        }
        else
        {
            var meetSelected = ReturnPickupStylePicker.SelectedItem?.ToString() == "Meet & Greet";
            ReturnPickupSignGrid.IsVisible = meetSelected;
        }
    }


    private async void OnBuildJson(object? sender, EventArgs e)
    {
        // ---- Basic required fields
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

        // Enforce pickup isn’t in the past / too soon (nice UX default)
        if (pickupDT <= DateTime.Now.AddMinutes(15))
        {
            await DisplayAlert("Pickup time", "Pickup should be at least 15 minutes from now.", "OK");
            return;
        }

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

        var isAirportPickup = IsAirportText(pickupLoc);

        var outboundStyle = PickupStyle.Curbside;
        string? outboundSign = null;

        if (isAirportPickup)
        {
            outboundStyle = (PickupStylePicker.SelectedItem?.ToString() == "Meet & Greet")
                ? PickupStyle.MeetAndGreet : PickupStyle.Curbside;
            outboundSign = (outboundStyle == PickupStyle.MeetAndGreet) ? (PickupSignEntry.Text ?? "").Trim() : null;
        }
        else
        {
            var wantsMeet = RequestsPicker.SelectedItem?.ToString() == ReqMeetAndGreet;
            outboundStyle = wantsMeet ? PickupStyle.MeetAndGreet : PickupStyle.Curbside;
            outboundSign = wantsMeet ? (NonAirportMeetSignEntry.Text ?? "").Trim() : null;
        }

        // Determine return pickup style/sign (if applicable)
        PickupStyle? returnStyle = null;
        string? returnSign = null;
        if (retDT is not null)
        {
            var retPickupLoc = dropLoc ?? pickupLoc;
            var isAirportReturnPickup = IsAirportText(retPickupLoc);

            if (isAirportReturnPickup)
            {
                returnStyle = (ReturnPickupStylePicker.SelectedItem?.ToString() == "Meet & Greet")
                    ? PickupStyle.MeetAndGreet : PickupStyle.Curbside;
                returnSign = (returnStyle == PickupStyle.MeetAndGreet) ? (ReturnPickupSignEntry.Text ?? "").Trim() : null;
            }
            else
            {
                // (Optional) expose another Additional Request for return meet & greet
                // For now default to Curbside unless you add a separate toggle for return.
                returnStyle = PickupStyle.Curbside;
                returnSign = null;
            }
        }

        // Pickup must exist; Dropoff required when not As Directed
        if (string.IsNullOrWhiteSpace(pickupLoc))
        {
            await DisplayAlert("Pickup", "Please select or enter a pickup location.", "OK");
            return;
        }
        if (!isAsDirected && string.IsNullOrWhiteSpace(dropLoc))
        {
            await DisplayAlert("Dropoff", "Please select or enter a dropoff location.", "OK");
            return;
        }

        // 1) Decide flight mode from picker
        var flightSel = FlightInfoPicker.SelectedItem?.ToString();
        var mode = flightSel == FlightOptionCommercial
            ? FlightMode.Commercial
            : flightSel == FlightOptionPrivate
                ? FlightMode.Private
                : FlightMode.None;

        // --- Enforce per-leg flight rules BEFORE building state ---
        if (mode == FlightMode.Commercial)
        {
            var outboundFlight = (FlightInfoEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(outboundFlight))
            {
                await DisplayAlert("Flight info", "Outbound flight number is required for commercial flights.", "OK");
                return;
            }

            if (retDT is not null)
            {
                var returnFlight = (ReturnFlightEntry.Text ?? "").Trim();
                if (string.IsNullOrEmpty(returnFlight))
                {
                    await DisplayAlert("Flight info", "Return flight number is required for the return leg (commercial).", "OK");
                    return;
                }
            }
        }
        else if (mode == FlightMode.Private)
        {
            var outboundTail = (FlightInfoEntry.Text ?? "").Trim();
            if (string.IsNullOrEmpty(outboundTail))
            {
                await DisplayAlert("Flight info", "Outbound tail number is required for private flights.", "OK");
                return;
            }

            if (retDT is not null && _allowReturnTailChange)
            {
                var returnTail = (ReturnFlightEntry.Text ?? "").Trim();
                if (string.IsNullOrEmpty(returnTail))
                {
                    await DisplayAlert("Flight info", "Return tail number is required when changing aircraft for the return leg.", "OK");
                    return;
                }
            }
        }

        // 2) Build the state (use your validated values)
        var state = new QuoteFormState
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
                PhoneNumber = string.IsNullOrWhiteSpace(PassengerPhone.Text) ? "" : PassengerPhone.Text,
                EmailAddress = string.IsNullOrWhiteSpace(PassengerEmail.Text) ? "" : PassengerEmail.Text
            },
            AdditionalPassengers = _additionalPassengers.ToList(),

            VehicleClass = VehiclePicker.SelectedItem?.ToString() ?? "Sedan",
            PickupDateTime = pickupDT,
            PickupLocation = pickupLoc,

            PickupStyle = outboundStyle,
            PickupSignText = outboundSign,
            ReturnPickupStyle = returnStyle,
            ReturnPickupSignText = returnSign,

            AsDirected = isAsDirected,
            Hours = isAsDirected ? (int?)Math.Max(1, (int)HoursStepper.Value) : null,
            DropoffLocation = isAsDirected ? null : dropLoc,

            RoundTrip = !isAsDirected && RoundTripCheck.IsChecked,
            ReturnPickupTime = retDT,

            AdditionalRequest = RequestsPicker.SelectedItem?.ToString(),
            AdditionalRequestOtherText = RequestOtherGrid.IsVisible ? (RequestOtherEntry.Text ?? "") : null,

            FlightMode = mode,

            // outbound flight inputs from the outbound field
            OutboundFlightNumber = (mode == FlightMode.Commercial) ? (FlightInfoEntry.Text ?? "").Trim() : null,
            OutboundTailNumber = (mode == FlightMode.Private) ? (FlightInfoEntry.Text ?? "").Trim() : null,

            // return flight inputs (from return controls)
            AllowReturnTailChange = _allowReturnTailChange,
            ReturnFlightNumber = (mode == FlightMode.Commercial && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null,
            ReturnTailNumber = (mode == FlightMode.Private && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null
        };

        // 3) Build the QuoteDraft via the service
        var draft = _draftBuilder.Build(state);

        // 4) Show JSON
        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
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
