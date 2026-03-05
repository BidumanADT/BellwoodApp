using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using System.Text.Json.Serialization;
using BellwoodGlobal.Core.Domain;
using BellwoodGlobal.Core.Helpers;
using BellwoodGlobal.Mobile.ViewModels;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookRidePage : ContentPage
{
    private readonly IProfileService _profile;
    private readonly ObservableCollection<string> _additionalPassengers = new();
    private readonly ITripDraftBuilder _draftBuilder;
    private readonly IAdminApi _adminApi;
    private readonly IPaymentService _paymentService;
    private readonly ILocationPickerService _locationPicker;
    private readonly IFormStateService _formStateService; // NEW: Phase 5 form persistence

    private List<PaymentMethod> _savedPaymentMethods = new();
    private const string PaymentMethodNew = "Add New Card";

    private const string PassengerSelf = "Myself";
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
    
    // NEW: Store selected locations from autocomplete (with coordinates)
    private Models.Location? _selectedPickupLocation;
    private Models.Location? _selectedDropoffLocation;
    
    private bool _allowReturnTailChange;
    private bool _requestsHasMeetOption;
    private bool _passengerCountDirty;
    private string? _suggestedVehicleClass;
    private bool _userChoseToKeep;
    private bool _pickersInitialized;

    // NEW: Phase 5 - Flag to prevent auto-save after successful submission
    private bool _submittedSuccessfully = false;

    public BookRidePage()
    {
        InitializeComponent();
        _profile = ServiceHelper.GetRequiredService<IProfileService>();
        _draftBuilder = ServiceHelper.GetRequiredService<ITripDraftBuilder>();
        _adminApi = ServiceHelper.GetRequiredService<IAdminApi>();
        _paymentService = ServiceHelper.GetRequiredService<IPaymentService>();
        _locationPicker = ServiceHelper.GetRequiredService<ILocationPickerService>();
        _formStateService = ServiceHelper.GetRequiredService<IFormStateService>(); // NEW: Phase 5

        // Booker fields populated asynchronously from OnAppearing → LoadBookerAsync().
        InitializeData();
        InitializePickers();
        InitializeDefaults();
        InitializeEventHandlers();

        // REMOVED: Don't load payment methods in constructor - move to OnAppearing
        // _ = LoadPaymentMethodsAsync();
    }

    /// <summary>
    /// Ensures the booker profile is fetched (if not already cached) and
    /// populates the read-only booker fields.  Shows an advisory label when
    /// the profile is missing or incomplete.
    /// </summary>
    private async Task LoadBookerAsync()
    {
        if (!_profile.IsProfileLoaded)
            await _profile.LoadProfileAsync();

        var booker = _profile.GetBooker();
        var hasProfile = booker is not null
            && (!string.IsNullOrWhiteSpace(booker.FirstName)
                || !string.IsNullOrWhiteSpace(booker.EmailAddress));

        if (hasProfile)
        {
            BookerFirst.Text = booker!.FirstName;
            BookerLast.Text  = booker.LastName;
            BookerPhone.Text = booker.PhoneNumber ?? "";
            BookerEmail.Text = booker.EmailAddress ?? "";
            BookerIncompleteLabel.IsVisible = false;
        }
        else
        {
            BookerFirst.Text = "";
            BookerLast.Text  = "";
            BookerPhone.Text = "";
            BookerEmail.Text = "";
            BookerIncompleteLabel.IsVisible = true;
        }
    }

    private void InitializeData()
    {
        _savedPassengers = _profile.GetSavedPassengers().ToList();
        _savedLocations = _profile.GetSavedLocations().ToList();
    }

    private void InitializePickers()
    {
        // ── Dynamic pickers: always clear + rebuild from current lists ──────────
        // Passenger
        PassengerPicker.SelectedIndexChanged -= OnPassengerChanged;
        PassengerPicker.Items.Clear();
        PassengerPicker.Items.Add(PassengerSelf);
        foreach (var p in _savedPassengers) PassengerPicker.Items.Add(p.ToString());
        PassengerPicker.Items.Add(PassengerNew);
        PassengerPicker.SelectedIndexChanged += OnPassengerChanged;

        // Pickup locations
        PickupLocationPicker.SelectedIndexChanged -= OnPickupLocationChanged;
        PickupLocationPicker.Items.Clear();
        foreach (var loc in _savedLocations) PickupLocationPicker.Items.Add(loc.ToString());
        PickupLocationPicker.Items.Add(LocationNew);
        PickupLocationPicker.SelectedIndexChanged += OnPickupLocationChanged;

        // Dropoff locations
        DropoffPicker.SelectedIndexChanged -= OnDropoffChanged;
        DropoffPicker.Items.Clear();
        DropoffPicker.Items.Add(AsDirected);
        foreach (var loc in _savedLocations) DropoffPicker.Items.Add(loc.ToString());
        DropoffPicker.Items.Add(LocationNew);
        DropoffPicker.SelectedIndexChanged += OnDropoffChanged;

        // ── Static pickers: only populate once (constructor call) ────────────
        if (_pickersInitialized) return;

        // Vehicle
        foreach (var v in new[] { "Sedan", "SUV", "Sprinter", "S-Class" }) VehiclePicker.Items.Add(v);
        VehiclePicker.SelectedIndex = 0;
        VehiclePicker.SelectedIndexChanged += OnVehicleChanged;

        // Flight
        FlightInfoPicker.Items.Add(FlightOptionTBD);
        FlightInfoPicker.Items.Add(FlightOptionCommercial);
        FlightInfoPicker.Items.Add(FlightOptionPrivate);
        FlightInfoPicker.SelectedIndexChanged += OnFlightInfoChanged;
        FlightInfoPicker.SelectedIndex = 0;

        // Requests
        foreach (var r in new[] { "Child Seats", "Accessible Vehicle", "Other" }) RequestsPicker.Items.Add(r);
        RequestsPicker.SelectedIndexChanged += (_, __) =>
        {
            var sel = RequestsPicker.SelectedItem?.ToString();
            RequestOtherGrid.IsVisible = sel == "Other";
            NonAirportMeetGrid.IsVisible = sel == ReqMeetAndGreet;
        };

        PickupStylePicker.SelectedIndexChanged += (_, __) => UpdatePickupStyleAirportUx();
        ReturnPickupStylePicker.SelectedIndexChanged += (_, __) => UpdateReturnPickupStyleAirportUx();

        AdditionalPassengersList.ItemsSource = _additionalPassengers;

        // Payment Source (items managed by LoadPaymentMethodsAsync)
        PaymentPicker.SelectedIndexChanged += OnPaymentPickerChanged;

        _pickersInitialized = true;
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

    // ===== EVENT HANDLERS =====
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
        var isNewLocation = PickupLocationPicker.SelectedItem?.ToString() == LocationNew;
        
        // Show autocomplete + manual entry when "New Location" selected
        PickupAutocompleteGrid.IsVisible = isNewLocation;
        PickupNewGrid.IsVisible = isNewLocation;
        
        // Clear previous autocomplete selection when switching
        if (isNewLocation)
        {
            _selectedPickupLocation = null;
            PickupAutocomplete.Clear();
        }
        
        UpdatePickupStyleAirportUx();
    }

    private void OnDropoffChanged(object? sender, EventArgs e)
    {
        var sel = DropoffPicker.SelectedItem?.ToString();
        var isAsDirected = sel == AsDirected;
        var isNewLocation = sel == LocationNew;

        AsDirectedHoursGrid.IsVisible = isAsDirected;
        RoundTripGrid.IsVisible = !isAsDirected;
        
        // Show autocomplete + manual entry when "New Location" selected
        DropoffAutocompleteGrid.IsVisible = isNewLocation;
        DropoffNewGrid.IsVisible = isNewLocation;

        // Clear previous autocomplete selection when switching
        if (isNewLocation)
        {
            _selectedDropoffLocation = null;
            DropoffAutocomplete.Clear();
        }

        if (isAsDirected) RoundTripCheck.IsChecked = false;

        ReturnSection.IsVisible = !isAsDirected && RoundTripCheck.IsChecked;
        if (ReturnSection.IsVisible) SyncReturnMinAndSuggest();

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
    }

    // NEW: Handle autocomplete selection for Pickup
    private void OnPickupAutocompleteSelected(object? sender, LocationSelectedEventArgs e)
    {
        var location = e.Location;
        
        // Store the location object (has coordinates)
        _selectedPickupLocation = location;
        
        // Populate manual entry fields
        PickupNewLabel.Text = location.Label;
        PickupNewAddress.Text = location.Address;
        
        // Update airport-specific UX
        UpdatePickupStyleAirportUx();
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[BookRidePage] Pickup autocomplete selected: {location.Label} @ {location.Latitude}, {location.Longitude}");
#endif
    }

    // NEW: Handle autocomplete selection for Dropoff
    private void OnDropoffAutocompleteSelected(object? sender, LocationSelectedEventArgs e)
    {
        var location = e.Location;
        
        // Store the location object (has coordinates)
        _selectedDropoffLocation = location;
        
        // Populate manual entry fields
        DropoffNewLabel.Text = location.Label;
        DropoffNewAddress.Text = location.Address;
        
        // Update return pickup airport UX (dropoff becomes return pickup in round trip)
        UpdateReturnPickupStyleAirportUx();
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[BookRidePage] Dropoff autocomplete selected: {location.Label} @ {location.Latitude}, {location.Longitude}");
#endif
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
        var phone = (PassengerPhone.Text ?? "").Trim();
        var email = (PassengerEmail.Text ?? "").Trim();
        var saved = await _profile.AddSavedPassengerAsync(first, last,
            string.IsNullOrEmpty(phone) ? null : phone,
            string.IsNullOrEmpty(email) ? null : email);
        if (saved is null)
        {
            await DisplayAlert("Error", "Could not save passenger. Please try again.", "OK");
            return;
        }
        _savedPassengers.Add(saved);
        var insertAt = Math.Max(1, PassengerPicker.Items.Count - 1);
        PassengerPicker.Items.Insert(insertAt, saved.ToString());
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
        
        // Preserve coordinates from autocomplete selection if available
        var pickupLat = _selectedPickupLocation?.Latitude ?? 0.0;
        var pickupLng = _selectedPickupLocation?.Longitude ?? 0.0;
        var savedPickup = await _profile.AddSavedLocationAsync(label, addr, pickupLat, pickupLng, isFavorite: false);
        if (savedPickup is null)
        {
            await DisplayAlert("Error", "Could not save pickup location. Please try again.", "OK");
            return;
        }
        _savedLocations.Add(savedPickup);
        var display = savedPickup.ToString();
        var insertAt = Math.Max(0, PickupLocationPicker.Items.Count - 1);
        PickupLocationPicker.Items.Insert(insertAt, display);
        PickupLocationPicker.SelectedIndex = insertAt;

        // Hide both autocomplete and manual entry grids
        PickupAutocompleteGrid.IsVisible = false;
        PickupNewGrid.IsVisible = false;

        await DisplayAlert("Saved", $"Pickup location added{(pickupLat != 0 || pickupLng != 0 ? " (with coordinates)" : "")}.", "OK");
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
        
        // Preserve coordinates from autocomplete selection if available
        var dropoffLat = _selectedDropoffLocation?.Latitude ?? 0.0;
        var dropoffLng = _selectedDropoffLocation?.Longitude ?? 0.0;
        var savedDropoff = await _profile.AddSavedLocationAsync(label, addr, dropoffLat, dropoffLng, isFavorite: false);
        if (savedDropoff is null)
        {
            await DisplayAlert("Error", "Could not save dropoff location. Please try again.", "OK");
            return;
        }
        _savedLocations.Add(savedDropoff);
        var display = savedDropoff.ToString();
        var insertAt = Math.Max(1, DropoffPicker.Items.Count - 1);
        DropoffPicker.Items.Insert(insertAt, display);
        DropoffPicker.SelectedIndex = insertAt;

        // Hide both autocomplete and manual entry grids
        DropoffAutocompleteGrid.IsVisible = false;
        DropoffNewGrid.IsVisible = false;

        await DisplayAlert("Saved", $"Dropoff location added{(dropoffLat != 0 || dropoffLng != 0 ? " (with coordinates)" : "")}.", "OK");
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

    private async Task LoadPaymentMethodsAsync()
    {
        try
        {
            _savedPaymentMethods = (await _paymentService.GetStoredPaymentMethodsAsync()).ToList();

            PaymentPicker.Items.Clear();
            foreach (var pm in _savedPaymentMethods)
                PaymentPicker.Items.Add(pm.DisplayName);

            PaymentPicker.Items.Add(PaymentMethodNew);

            // Pre-select first card if available
            if (_savedPaymentMethods.Any())
                PaymentPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Payment Methods", $"Could not load payment methods: {ex.Message}", "OK");
        }
    }

    private void OnPaymentPickerChanged(object? sender, EventArgs e)
    {
        var sel = PaymentPicker.SelectedItem?.ToString();
        NewPaymentGrid.IsVisible = sel == PaymentMethodNew;
    }

    private void OnCardNumberTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;

        // Auto-format with spaces: 1234 5678 9012 3456
        var digitsOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length > 16)
            digitsOnly = digitsOnly[..16];

        var formatted = string.Join(" ", Enumerable.Range(0, (digitsOnly.Length + 3) / 4)
            .Select(i => digitsOnly.Substring(i * 4, Math.Min(4, digitsOnly.Length - i * 4))));

        if (formatted != e.NewTextValue)
            entry.Text = formatted;
    }

    private void OnExpiryDateTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry) return;

        // Auto-format: MM/YY
        var digitsOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length > 4)
            digitsOnly = digitsOnly[..4];

        var formatted = digitsOnly.Length > 2
            ? $"{digitsOnly[..2]}/{digitsOnly[2..]}"
            : digitsOnly;

        if (formatted != e.NewTextValue)
        {
            entry.Text = formatted;
        }
    }

    private static string DetectCardBrand(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 2)
            return "Unknown";

        var first2 = cardNumber[..2];
        var firstDigit = cardNumber[0];

        return firstDigit switch
        {
            '4' => "Visa",
            '5' when first2[1] >= '1' && first2[1] <= '5' => "Mastercard",
            '3' when first2 == "34" || first2 == "37" => "Amex",
            '6' when first2 == "60" || first2 == "65" => "Discover",
            _ => "Unknown"
        };
    }

    private async void OnSaveNewPaymentMethod(object? sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(CardholderNameEntry.Text))
        {
            await DisplayAlert("Required", "Cardholder name is required.", "OK");
            return;
        }

        var cardNumber = new string(CardNumberEntry.Text?.Where(char.IsDigit).ToArray() ?? []);
        if (cardNumber.Length != 16)
        {
            await DisplayAlert("Invalid Card", "Card number must be 16 digits.", "OK");
            return;
        }

        var expiryParts = ExpiryDateEntry.Text?.Split('/') ?? [];
        if (expiryParts.Length != 2 ||
            !int.TryParse(expiryParts[0], out var mm) ||
            !int.TryParse(expiryParts[1], out var yy))
        {
            await DisplayAlert("Invalid Expiry", "Expiration date must be MM/YY format.", "OK");
            return;
        }

        var year = 2000 + yy; // Assume 20xx
        if (new DateTime(year, mm, 1) < DateTime.Now)
        {
            await DisplayAlert("Expired Card", "This card has already expired.", "OK");
            return;
        }

        var cvc = CvcEntry.Text ?? "";
        if (cvc.Length < 3 || cvc.Length > 4)
        {
            await DisplayAlert("Invalid CVC", "CVC must be 3-4 digits.", "OK");
            return;
        }

        var zip = BillingZipEntry.Text ?? "";
        if (zip.Length != 5)
        {
            await DisplayAlert("Invalid ZIP", "Billing ZIP code must be 5 digits.", "OK");
            return;
        }

        try
        {
            // Step 1: Tokenize card with Stripe
            var token = await _paymentService.TokenizeCardAsync(cardNumber, mm, year, cvc);
            var last4 = cardNumber[^4..];

            // Step 2: Submit token + metadata to backend
            var request = new NewCardRequest
            {
                NameOnCard = CardholderNameEntry.Text!,
                BillingZip = zip,
                StripeToken = token,
                Last4 = last4,
                Brand = DetectCardBrand(cardNumber)
            };

            var newMethod = await _paymentService.SubmitPaymentMethodAsync(request);

            // Step 3: Add to picker
            _savedPaymentMethods.Add(newMethod);
            PaymentPicker.Items.Insert(PaymentPicker.Items.Count - 1, newMethod.DisplayName);
            PaymentPicker.SelectedIndex = PaymentPicker.Items.Count - 2; // Select the new card

            // Clear form
            NewPaymentGrid.IsVisible = false;
            CardholderNameEntry.Text = "";
            CardNumberEntry.Text = "";
            ExpiryDateEntry.Text = "";
            CvcEntry.Text = "";
            BillingZipEntry.Text = "";

            await DisplayAlert("Success", "Payment method added successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not add payment method: {ex.Message}", "OK");
        }
    }

    // ===== MAIN SUBMISSION HANDLER (KEY DIFFERENCE FROM QUOTEPAGE) =====
    private async void OnRequestBooking(object? sender, EventArgs e)
    {
        // Validation
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

        if (PaymentPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Required", "Please select a payment method.", "OK");
            return;
        }

        if (PaymentPicker.SelectedItem?.ToString() == PaymentMethodNew)
        {
            await DisplayAlert("Required", "Please save your new payment method before booking.", "OK");
            return;
        }
        var selectedPaymentMethod = _savedPaymentMethods[PaymentPicker.SelectedIndex];

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
            ReturnTailNumber = (mode == FlightMode.Private && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null,
            PaymentMethodId = selectedPaymentMethod.Id,
            PaymentMethodLast4 = selectedPaymentMethod.Last4
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
            
            // NEW: Set flag to prevent auto-save and clear saved form state
            _submittedSuccessfully = true;
            await _formStateService.ClearBookingFormStateAsync();
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[BookRidePage] Form state cleared after successful submission");
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

    // ===== PHASE 5: FORM STATE PERSISTENCE =====

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure booker profile is loaded from AdminAPI and displayed.
        await LoadBookerAsync();

        // Load saved passengers and locations from AdminAPI (with offline cache fallback).
        await _profile.LoadSavedPassengersAsync();
        await _profile.LoadSavedLocationsAsync();
        _savedPassengers = _profile.GetSavedPassengers().ToList();
        _savedLocations  = _profile.GetSavedLocations().ToList();
        InitializePickers();

        // Load payment methods FIRST (before checking for draft)
        await LoadPaymentMethodsAsync();

        // Check for saved form state
        if (_formStateService.HasSavedBookingForm())
        {
            var shouldRestore = await DisplayAlert(
                "Restore Draft?",
                "You have an unsaved booking. Would you like to continue where you left off?",
                "Yes, Restore",
                "No, Start Fresh"
            );

            if (shouldRestore)
            {
                await RestoreFormStateAsync();
            }
            else
            {
                await _formStateService.ClearBookingFormStateAsync();
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // NEW: Phase 5 - Don't save if form was successfully submitted
        if (_submittedSuccessfully)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[BookRidePage] Skipping auto-save (form was successfully submitted)");
#endif
            return;
        }

        // Auto-save form state (fire and forget)
        _ = SaveFormStateAsync();
    }

    private async Task SaveFormStateAsync()
    {
        try
        {
            var state = new BookRidePageState
            {
                // Pickup Location
                PickupLocationIndex = PickupLocationPicker.SelectedIndex >= 0 ? PickupLocationPicker.SelectedIndex : null,
                PickupNewLabel = PickupNewLabel.Text,
                PickupNewAddress = PickupNewAddress.Text,
                PickupLatitude = _selectedPickupLocation?.Latitude,
                PickupLongitude = _selectedPickupLocation?.Longitude,
                PickupPlaceId = _selectedPickupLocation?.PlaceId,

                // Dropoff Location
                DropoffSelection = DropoffPicker.SelectedItem?.ToString(),
                DropoffLocationIndex = DropoffPicker.SelectedIndex >= 0 ? DropoffPicker.SelectedIndex : null,
                DropoffNewLabel = DropoffNewLabel.Text,
                DropoffNewAddress = DropoffNewAddress.Text,
                DropoffLatitude = _selectedDropoffLocation?.Latitude,
                DropoffLongitude = _selectedDropoffLocation?.Longitude,
                DropoffPlaceId = _selectedDropoffLocation?.PlaceId,

                // Date/Time
                PickupDate = PickupDate.Date,
                PickupTime = PickupTime.Time,
                ReturnDate = ReturnDatePicker.Date,
                ReturnTime = ReturnTimePicker.Time,

                // Vehicle & Passenger
                VehiclePickerIndex = VehiclePicker.SelectedIndex >= 0 ? VehiclePicker.SelectedIndex : null,
                PassengerPickerIndex = PassengerPicker.SelectedIndex >= 0 ? PassengerPicker.SelectedIndex : null,
                PassengerFirstName = PassengerFirst.Text,
                PassengerLastName = PassengerLast.Text,
                PassengerPhone = PassengerPhone.Text,
                PassengerEmail = PassengerEmail.Text,

                // Additional Passengers
                AdditionalPassengers = _additionalPassengers.ToList(),

                // Round Trip
                RoundTrip = RoundTripCheck.IsChecked,

                // Luggage
                PassengerCount = (int)PassengerCountStepper.Value,
                CheckedBags = (int)CheckedBagsStepper.Value,
                CarryOnBags = (int)CarryOnBagsStepper.Value,

                // As Directed
                AsDirected = DropoffPicker.SelectedItem?.ToString() == AsDirected,
                Hours = (int)HoursStepper.Value,

                // Flight Info
                FlightInfoPickerIndex = FlightInfoPicker.SelectedIndex >= 0 ? FlightInfoPicker.SelectedIndex : null,
                FlightInfoEntry = FlightInfoEntry.Text,
                ReturnFlightEntry = ReturnFlightEntry.Text,
                AllowReturnTailChange = _allowReturnTailChange,

                // Pickup Style & Sign
                PickupStylePickerIndex = PickupStylePicker.SelectedIndex >= 0 ? PickupStylePicker.SelectedIndex : null,
                PickupSignEntry = PickupSignEntry.Text,
                ReturnPickupStylePickerIndex = ReturnPickupStylePicker.SelectedIndex >= 0 ? ReturnPickupStylePicker.SelectedIndex : null,
                ReturnPickupSignEntry = ReturnPickupSignEntry.Text,

                // Requests
                RequestsPickerIndex = RequestsPicker.SelectedIndex >= 0 ? RequestsPicker.SelectedIndex : null,
                RequestOtherEntry = RequestOtherEntry.Text,
                NonAirportMeetSignEntry = NonAirportMeetSignEntry.Text,

                // Autocomplete in-progress text
                AutocompleteSearchText_Pickup = PickupAutocomplete.SearchText,
                AutocompleteSearchText_Dropoff = DropoffAutocomplete.SearchText,

                // Payment (index only, never store card numbers!)
                PaymentPickerIndex = PaymentPicker.SelectedIndex >= 0 ? PaymentPicker.SelectedIndex : null,
                NewCardHolderName = CardholderNameEntry.Text,

                LastModified = DateTime.UtcNow
            };

            await _formStateService.SaveBookingFormStateAsync(state);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("[BookRidePage] Form state saved");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[BookRidePage] Error saving form state: {ex.Message}");
#endif
        }
    }

    private async Task RestoreFormStateAsync()
    {
        try
        {
            var state = await _formStateService.LoadBookingFormStateAsync();
            if (state == null) return;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[BookRidePage] Restoring form state from {state.LastModified}");
#endif

            // Restore pickers
            if (state.PickupLocationIndex.HasValue && state.PickupLocationIndex.Value < PickupLocationPicker.Items.Count)
                PickupLocationPicker.SelectedIndex = state.PickupLocationIndex.Value;

            if (state.VehiclePickerIndex.HasValue && state.VehiclePickerIndex.Value < VehiclePicker.Items.Count)
                VehiclePicker.SelectedIndex = state.VehiclePickerIndex.Value;

            if (state.PassengerPickerIndex.HasValue && state.PassengerPickerIndex.Value < PassengerPicker.Items.Count)
                PassengerPicker.SelectedIndex = state.PassengerPickerIndex.Value;

            if (state.FlightInfoPickerIndex.HasValue && state.FlightInfoPickerIndex.Value < FlightInfoPicker.Items.Count)
                FlightInfoPicker.SelectedIndex = state.FlightInfoPickerIndex.Value;

            if (state.RequestsPickerIndex.HasValue && state.RequestsPickerIndex.Value < RequestsPicker.Items.Count)
                RequestsPicker.SelectedIndex = state.RequestsPickerIndex.Value;

            // Restore Dropoff picker
            if (!string.IsNullOrWhiteSpace(state.DropoffSelection))
            {
                var dropoffIndex = DropoffPicker.Items.IndexOf(state.DropoffSelection);
                if (dropoffIndex >= 0)
                    DropoffPicker.SelectedIndex = dropoffIndex;
            }
            else if (state.DropoffLocationIndex.HasValue && state.DropoffLocationIndex.Value < DropoffPicker.Items.Count)
            {
                DropoffPicker.SelectedIndex = state.DropoffLocationIndex.Value;
            }

            // Restore Payment picker (wait for payment methods to load first)
            if (_savedPaymentMethods.Any() && state.PaymentPickerIndex.HasValue && state.PaymentPickerIndex.Value < PaymentPicker.Items.Count)
                PaymentPicker.SelectedIndex = state.PaymentPickerIndex.Value;

            // Restore text fields
            PickupNewLabel.Text = state.PickupNewLabel;
            PickupNewAddress.Text = state.PickupNewAddress;
            DropoffNewLabel.Text = state.DropoffNewLabel;
            DropoffNewAddress.Text = state.DropoffNewAddress;

            PassengerFirst.Text = state.PassengerFirstName;
            PassengerLast.Text = state.PassengerLastName;
            PassengerPhone.Text = state.PassengerPhone;
            PassengerEmail.Text = state.PassengerEmail;

            FlightInfoEntry.Text = state.FlightInfoEntry;
            ReturnFlightEntry.Text = state.ReturnFlightEntry;
            PickupSignEntry.Text = state.PickupSignEntry;
            ReturnPickupSignEntry.Text = state.ReturnPickupSignEntry;
            RequestOtherEntry.Text = state.RequestOtherEntry;
            NonAirportMeetSignEntry.Text = state.NonAirportMeetSignEntry;

            CardholderNameEntry.Text = state.NewCardHolderName;

            // Restore coordinates
            if (state.PickupLatitude.HasValue && state.PickupLongitude.HasValue)
            {
                _selectedPickupLocation = new Models.Location
                {
                    Label = state.PickupNewLabel ?? "",
                    Address = state.PickupNewAddress ?? "",
                    Latitude = state.PickupLatitude.Value,
                    Longitude = state.PickupLongitude.Value,
                    PlaceId = state.PickupPlaceId,
                    IsVerified = true
                };

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[BookRidePage] Restored pickup coordinates: {_selectedPickupLocation.Latitude}, {_selectedPickupLocation.Longitude}");
#endif
            }

            if (state.DropoffLatitude.HasValue && state.DropoffLongitude.HasValue)
            {
                _selectedDropoffLocation = new Models.Location
                {
                    Label = state.DropoffNewLabel ?? "",
                    Address = state.DropoffNewAddress ?? "",
                    Latitude = state.DropoffLatitude.Value,
                    Longitude = state.DropoffLongitude.Value,
                    PlaceId = state.DropoffPlaceId,
                    IsVerified = true
                };

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[BookRidePage] Restored dropoff coordinates: {_selectedDropoffLocation.Latitude}, {_selectedDropoffLocation.Longitude}");
#endif
            }

            // Restore date/time
            if (state.PickupDate.HasValue)
                PickupDate.Date = state.PickupDate.Value;
            if (state.PickupTime.HasValue)
                PickupTime.Time = state.PickupTime.Value;
            if (state.ReturnDate.HasValue)
                ReturnDatePicker.Date = state.ReturnDate.Value;
            if (state.ReturnTime.HasValue)
                ReturnTimePicker.Time = state.ReturnTime.Value;

            // Restore checkboxes/switches
            RoundTripCheck.IsChecked = state.RoundTrip;
            ReturnTailChangeSwitch.IsToggled = state.AllowReturnTailChange;

            // Restore steppers
            _suppressPassengerCountEvents = true;
            PassengerCountStepper.Value = state.PassengerCount;
            CheckedBagsStepper.Value = state.CheckedBags;
            CarryOnBagsStepper.Value = state.CarryOnBags;
            HoursStepper.Value = state.Hours;
            _suppressPassengerCountEvents = false;

            // Update labels
            PassengerCountValueLabel.Text = state.PassengerCount.ToString();
            CheckedBagsValueLabel.Text = state.CheckedBags.ToString();
            CarryOnBagsValueLabel.Text = state.CarryOnBags.ToString();
            HoursValueLabel.Text = state.Hours.ToString();

            // Restore additional passengers
            _additionalPassengers.Clear();
            if (state.AdditionalPassengers != null)
            {
                foreach (var pax in state.AdditionalPassengers)
                    _additionalPassengers.Add(pax);
            }

            // Restore pickup style pickers
            if (state.PickupStylePickerIndex.HasValue && state.PickupStylePickerIndex.Value < PickupStylePicker.Items.Count)
                PickupStylePicker.SelectedIndex = state.PickupStylePickerIndex.Value;

            if (state.ReturnPickupStylePickerIndex.HasValue && state.ReturnPickupStylePickerIndex.Value < ReturnPickupStylePicker.Items.Count)
                ReturnPickupStylePicker.SelectedIndex = state.ReturnPickupStylePickerIndex.Value;

            // Update UI based on restored state
            UpdatePickupStyleAirportUx();
            UpdateReturnPickupStyleAirportUx();
            UpdateReturnFlightUx();
            ReevaluateCapacityAndMaybeShowBanner();

            await DisplayAlert("Draft Restored", "Your booking has been restored.", "OK");
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[BookRidePage] Error restoring form state: {ex.Message}");
#endif
            await DisplayAlert("Restore Failed", "Could not restore your draft. Starting fresh.", "OK");
        }
    }
}