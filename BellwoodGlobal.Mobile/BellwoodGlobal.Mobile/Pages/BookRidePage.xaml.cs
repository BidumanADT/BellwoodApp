using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using System.Text.Json.Serialization;
using BellwoodGlobal.Core.Domain;
using BellwoodGlobal.Core.Helpers;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookRidePage : ContentPage
{
    private readonly IProfileService _profile;
    private readonly ObservableCollection<string> _additionalPassengers = new();
    private readonly ITripDraftBuilder _draftBuilder;
    private readonly IAdminApi _adminApi;

    private const string PassengerSelf = "Booker (you)";
    private const string PassengerNew = "New Passenger";
    private const string LocationNew = "New Location";
    private const string AsDirected = "As Directed";
    private const string FlightOptionTBD = "TBD";
    private const string FlightOptionCommercial = "Commercial Flight";
    private const string FlightOptionPrivate = "Private Tail Number";
    private const string ReqMeetAndGreet = "Meet & Greet";

    private bool _suppressPassengerCountEvents;
    private List<Passenger> _savedPassengers = new();
    private List<Models.Location> _savedLocations = new();
    private bool _allowReturnTailChange;
    private bool _requestsHasMeetOption;
    private bool _passengerCountDirty;
    private string? _suggestedVehicleClass;
    private bool _userChoseToKeep;

    public BookRidePage()
    {
        InitializeComponent();
        _profile = ServiceHelper.GetRequiredService<IProfileService>();
        _draftBuilder = ServiceHelper.GetRequiredService<ITripDraftBuilder>();
        _adminApi = ServiceHelper.GetRequiredService<IAdminApi>();

        InitializeBooker();
        InitializeData();
        InitializePickers();
        InitializeDefaults();
        InitializeEventHandlers();
    }

    private void InitializeBooker()
    {
        var booker = _profile.GetBooker();
        BookerFirst.Text = booker.FirstName;
        BookerLast.Text = booker.LastName;
        BookerPhone.Text = booker.PhoneNumber;
        BookerEmail.Text = booker.EmailAddress;
    }

    private void InitializeData()
    {
        _savedPassengers = _profile.GetSavedPassengers().ToList();
        _savedLocations = _profile.GetSavedLocations().ToList();
    }

    private void InitializePickers()
    {
        // Passenger
        PassengerPicker.Items.Add(PassengerSelf);
        foreach (var p in _savedPassengers) PassengerPicker.Items.Add(p.ToString());
        PassengerPicker.Items.Add(PassengerNew);
        PassengerPicker.SelectedIndexChanged += OnPassengerChanged;

        // Vehicle
        foreach (var v in new[] { "Sedan", "SUV", "Sprinter", "S-Class" }) VehiclePicker.Items.Add(v);
        VehiclePicker.SelectedIndex = 0;
        VehiclePicker.SelectedIndexChanged += OnVehicleChanged;

        // Pickup
        foreach (var loc in _savedLocations) PickupLocationPicker.Items.Add(loc.ToString());
        PickupLocationPicker.Items.Add(LocationNew);
        PickupLocationPicker.SelectedIndexChanged += OnPickupLocationChanged;

        PickupStylePicker.SelectedIndexChanged += (_, __) => UpdatePickupStyleAirportUx();
        ReturnPickupStylePicker.SelectedIndexChanged += (_, __) => UpdateReturnPickupStyleAirportUx();

        // Flight
        FlightInfoPicker.Items.Add(FlightOptionTBD);
        FlightInfoPicker.Items.Add(FlightOptionCommercial);
        FlightInfoPicker.Items.Add(FlightOptionPrivate);
        FlightInfoPicker.SelectedIndexChanged += OnFlightInfoChanged;
        FlightInfoPicker.SelectedIndex = 0;

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
    }

    private void InitializeDefaults()
    {
        var now = DateTime.Now.AddMinutes(30);
        PickupDate.Date = now.Date;
        PickupTime.Time = now.TimeOfDay;

        _suppressPassengerCountEvents = true;
        PassengerCountStepper.Value = Math.Max(1, _additionalPassengers.Count + 1);
        PassengerCountValueLabel.Text = $"{(int)PassengerCountStepper.Value}";
        CheckedBagsValueLabel.Text = "0";
        CarryOnBagsValueLabel.Text = "0";
        HoursValueLabel.Text = "2";
        _suppressPassengerCountEvents = false;
    }

    private void InitializeEventHandlers()
    {
        PickupDate.DateSelected += (_, __) => SyncReturnMinAndSuggest();
        PickupTime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) SyncReturnMinAndSuggest(); };
        ReturnDatePicker.DateSelected += (_, __) => EnsureReturnAfterPickup();
        ReturnTimePicker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) EnsureReturnAfterPickup(); };

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
        ReevaluateCapacityAndMaybeShowBanner();
    }

    // ===== EVENT HANDLERS (same as QuotePage) =====
    private void OnPassengerChanged(object? sender, EventArgs e)
    {
        var sel = PassengerPicker.SelectedItem?.ToString();
        if (sel == PassengerNew)
        {
            PassengerNewGrid.IsVisible = true;
            SavePassengerButton.IsVisible = true;
            PassengerFirst.Text = "";
            PassengerLast.Text = "";
            PassengerPhone.Text = "";
            PassengerEmail.Text = "";
            return;
        }

        SavePassengerButton.IsVisible = false;
        PassengerNewGrid.IsVisible = true;

        if (sel == PassengerSelf)
        {
            PassengerFirst.Text = BookerFirst.Text;
            PassengerLast.Text = BookerLast.Text;
            PassengerPhone.Text = "";
            PassengerEmail.Text = "";
        }
        else if (!string.IsNullOrEmpty(sel))
        {
            var p = _savedPassengers.FirstOrDefault(x => x.ToString() == sel);
            if (p != null)
            {
                PassengerFirst.Text = p.FirstName;
                PassengerLast.Text = p.LastName;
                PassengerPhone.Text = p.PhoneNumber;
                PassengerEmail.Text = p.EmailAddress;
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
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Flight number";
            FlightInfoEntry.Placeholder = "e.g., AA1234";
            if (ReturnTailChangeSwitch.IsToggled) ReturnTailChangeSwitch.IsToggled = false;
            _allowReturnTailChange = false;
        }
        else if (sel == FlightOptionPrivate)
        {
            FlightInfoGrid.IsVisible = true;
            FlightInfoLabel.Text = "Tail number";
            FlightInfoEntry.Placeholder = "e.g., N123AB";
        }
        else
        {
            FlightInfoGrid.IsVisible = false;
            FlightInfoEntry.Text = string.Empty;
            ReturnFlightEntry.Text = "";
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

        ReturnTailChangeRow.IsVisible = isRoundTrip && isPrivate;

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
        UpdateReturnFlightUx();
    }

    private void OnAddAdditionalPassenger(object? sender, EventArgs e)
    {
        var name = (AdditionalPassengerEntry.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(name))
        {
            _additionalPassengers.Add(name);
            AdditionalPassengerEntry.Text = "";
        }
        SetDefaultPassengerCountFromList();
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnRemoveAdditionalPassenger(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is string name)
            _additionalPassengers.Remove(name);
        SetDefaultPassengerCountFromList();
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void EnsureRequestsMeetOptionVisible(bool visible)
    {
        if (visible && !_requestsHasMeetOption)
        {
            RequestsPicker.Items.Insert(0, ReqMeetAndGreet);
            _requestsHasMeetOption = true;
        }
        else if (!visible && _requestsHasMeetOption)
        {
            if (RequestsPicker.SelectedItem?.ToString() == ReqMeetAndGreet)
            {
                RequestsPicker.SelectedIndex = -1;
                NonAirportMeetGrid.IsVisible = false;
                NonAirportMeetSignEntry.Text = string.Empty;
            }
            var idx = RequestsPicker.Items.IndexOf(ReqMeetAndGreet);
            if (idx >= 0) RequestsPicker.Items.RemoveAt(idx);
            _requestsHasMeetOption = false;
        }
    }

    private void UpdatePickupStyleAirportUx()
    {
        var pickupLoc = LocationHelper.ResolveLocation(
            PickupLocationPicker.SelectedItem?.ToString(),
            PickupNewLabel.Text,
            PickupNewAddress.Text);
        var isAirportPickup = LocationHelper.IsAirportText(pickupLoc);

        PickupStyleRow.IsVisible = isAirportPickup;
        if (!isAirportPickup)
        {
            PickupSignGrid.IsVisible = false;
            PickupStylePicker.SelectedIndex = 0;
        }
        EnsureRequestsMeetOptionVisible(!isAirportPickup);

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

        var dropLoc = LocationHelper.ResolveLocation(
            DropoffPicker.SelectedItem?.ToString(),
            DropoffNewLabel.Text,
            DropoffNewAddress.Text);
        var isAirportReturnPickup = LocationHelper.IsAirportText(dropLoc);

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

    private void SetDefaultPassengerCountFromList()
    {
        if (_passengerCountDirty) return;
        var suggested = Math.Max(1, _additionalPassengers.Count + 1);
        _suppressPassengerCountEvents = true;
        PassengerCountStepper.Value = suggested;
        PassengerCountValueLabel.Text = $"{suggested}";
        _suppressPassengerCountEvents = false;
    }

    private void OnVehicleChanged(object? sender, EventArgs e)
    {
        _userChoseToKeep = false;
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnPassengerCountChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_suppressPassengerCountEvents) return;
        _passengerCountDirty = true;
        PassengerCountValueLabel.Text = $"{(int)e.NewValue}";
        _userChoseToKeep = false;
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnCheckedBagsChanged(object? sender, ValueChangedEventArgs e)
    {
        CheckedBagsValueLabel.Text = $"{(int)e.NewValue}";
        _userChoseToKeep = false;
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnCarryOnBagsChanged(object? sender, ValueChangedEventArgs e)
    {
        CarryOnBagsValueLabel.Text = $"{(int)e.NewValue}";
        _userChoseToKeep = false;
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnHoursChanged(object? sender, ValueChangedEventArgs e)
        => HoursValueLabel.Text = $"{(int)e.NewValue}";

    private async void OnSaveNewPassenger(object? sender, EventArgs e)
    {
        var first = (PassengerFirst.Text ?? "").Trim();
        var last = (PassengerLast.Text ?? "").Trim();
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(last))
        {
            await DisplayAlert("Passenger", "First and last name are required.", "OK");
            return;
        }
        var p = new Passenger
        {
            FirstName = first,
            LastName = last,
            PhoneNumber = (PassengerPhone.Text ?? "").Trim(),
            EmailAddress = (PassengerEmail.Text ?? "").Trim()
        };
        _savedPassengers.Add(p);
        var insertAt = Math.Max(1, PassengerPicker.Items.Count - 1);
        PassengerPicker.Items.Insert(insertAt, p.ToString());
        PassengerPicker.SelectedIndex = insertAt;
        PassengerNewGrid.IsVisible = false;
        await DisplayAlert("Saved", "Passenger added.", "OK");
    }

    private async void OnSaveNewPickup(object? sender, EventArgs e)
    {
        var label = (PickupNewLabel.Text ?? "").Trim();
        var addr = (PickupNewAddress.Text ?? "").Trim();
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(addr))
        {
            await DisplayAlert("Pickup", "Label and address are required.", "OK");
            return;
        }
        var loc = new Models.Location { Label = label, Address = addr };
        _savedLocations.Add(loc);
        var display = loc.ToString();
        var insertAt = Math.Max(0, PickupLocationPicker.Items.Count - 1);
        PickupLocationPicker.Items.Insert(insertAt, display);
        PickupLocationPicker.SelectedIndex = insertAt;
        PickupNewGrid.IsVisible = false;
        await DisplayAlert("Saved", "Pickup location added.", "OK");
        UpdatePickupStyleAirportUx();
    }

    private async void OnSaveNewDropoff(object? sender, EventArgs e)
    {
        var label = (DropoffNewLabel.Text ?? "").Trim();
        var addr = (DropoffNewAddress.Text ?? "").Trim();
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(addr))
        {
            await DisplayAlert("Dropoff", "Label and address are required.", "OK");
            return;
        }
        var loc = new Models.Location { Label = label, Address = addr };
        _savedLocations.Add(loc);
        var display = loc.ToString();
        var insertAt = Math.Max(1, DropoffPicker.Items.Count - 1);
        DropoffPicker.Items.Insert(insertAt, display);
        DropoffPicker.SelectedIndex = insertAt;
        DropoffNewGrid.IsVisible = false;
        await DisplayAlert("Saved", "Dropoff location added.", "OK");
        UpdateReturnPickupStyleAirportUx();
    }

    private void OnAcceptCapacitySuggestion(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_suggestedVehicleClass))
            VehiclePicker.SelectedItem = _suggestedVehicleClass;
        _userChoseToKeep = false;
        CapacityBanner.IsVisible = false;
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnKeepCurrentVehicle(object? sender, EventArgs e)
    {
        _userChoseToKeep = true;
        CapacityBanner.IsVisible = false;
    }

    private void ReevaluateCapacityAndMaybeShowBanner()
    {
        var cls = VehiclePicker.SelectedItem?.ToString() ?? "Sedan";
        var pax = (int)PassengerCountStepper.Value;
        var chk = (int)CheckedBagsStepper.Value;
        var car = (int)CarryOnBagsStepper.Value;

        var (within, note, suggestion) = CapacityValidator.Evaluate(pax, chk, car, cls);
        _suggestedVehicleClass = suggestion;

        if (_userChoseToKeep)
        {
            CapacityBanner.IsVisible = false;
            return;
        }

        if (!within && !string.IsNullOrWhiteSpace(suggestion))
        {
            CapacityBannerText.Text = $"Looks tight for {cls}. Suggested: {suggestion}. (pax={pax}, checked={chk}, carry-on={car})";
            CapacityBanner.IsVisible = true;
        }
        else
        {
            CapacityBanner.IsVisible = false;
        }
    }

    // ===== MAIN SUBMISSION HANDLER (KEY DIFFERENCE FROM QUOTEPAGE) =====
    private async void OnRequestBooking(object? sender, EventArgs e)
    {
        // Validation (same as QuotePage)
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

        var pickupLoc = LocationHelper.ResolveLocation(
            PickupLocationPicker.SelectedItem?.ToString(),
            PickupNewLabel.Text,
            PickupNewAddress.Text);
        string? dropLoc = isAsDirected ? null : LocationHelper.ResolveLocation(
            DropoffPicker.SelectedItem?.ToString(),
            DropoffNewLabel.Text,
            DropoffNewAddress.Text);

        var isAirportPickup = LocationHelper.IsAirportText(pickupLoc);
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

        PickupStyle? returnStyle = null;
        string? returnSign = null;
        if (retDT is not null)
        {
            var retPickupLoc = dropLoc ?? pickupLoc;
            var isAirportReturnPickup = LocationHelper.IsAirportText(retPickupLoc);

            if (isAirportReturnPickup)
            {
                returnStyle = (ReturnPickupStylePicker.SelectedItem?.ToString() == "Meet & Greet")
                    ? PickupStyle.MeetAndGreet : PickupStyle.Curbside;
                returnSign = (returnStyle == PickupStyle.MeetAndGreet) ? (ReturnPickupSignEntry.Text ?? "").Trim() : null;
            }
            else
            {
                returnStyle = PickupStyle.Curbside;
                returnSign = null;
            }
        }

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

        var flightSel = FlightInfoPicker.SelectedItem?.ToString();
        var mode = flightSel == FlightOptionCommercial ? FlightMode.Commercial
            : flightSel == FlightOptionPrivate ? FlightMode.Private : FlightMode.None;

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

        // Build state
        var state = new TripFormState
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
            OutboundFlightNumber = (mode == FlightMode.Commercial) ? (FlightInfoEntry.Text ?? "").Trim() : null,
            OutboundTailNumber = (mode == FlightMode.Private) ? (FlightInfoEntry.Text ?? "").Trim() : null,
            AllowReturnTailChange = _allowReturnTailChange,
            ReturnFlightNumber = (mode == FlightMode.Commercial && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null,
            ReturnTailNumber = (mode == FlightMode.Private && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null
        };

        state.PassengerCount = (int)PassengerCountStepper.Value;
        state.CheckedBags = (int)CheckedBagsStepper.Value;
        state.CarryOnBags = (int)CarryOnBagsStepper.Value;

        {
            var cls = state.VehicleClass ?? "Sedan";
            var (within, note, suggestion) = CapacityValidator.Evaluate(
                state.PassengerCount, state.CheckedBags ?? 0, state.CarryOnBags ?? 0, cls);

            state.CapacityWithinLimits = within;
            state.CapacityNote = note;
            state.SuggestedVehicle = suggestion;
            state.CapacityOverrideByUser = _userChoseToKeep;
        }

        var draft = _draftBuilder.Build(state);

        try
        {
            await _adminApi.SubmitBookingAsync(draft);

#if DEBUG
            var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            JsonEditor.Text = json;
            JsonFrame.IsVisible = true;
#endif

            await DisplayAlert("Booking Requested",
                "Your booking request has been submitted! Bellwood will confirm shortly.", "OK");

            // Navigate to Bookings dashboard
            await Shell.Current.GoToAsync(nameof(BookingsPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", $"Could not submit booking: {ex.Message}", "OK");
        }
    }

    private async void OnCopyJson(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(JsonEditor.Text ?? "");
        await DisplayAlert("Copied", "Booking JSON copied to clipboard.", "OK");
    }
}