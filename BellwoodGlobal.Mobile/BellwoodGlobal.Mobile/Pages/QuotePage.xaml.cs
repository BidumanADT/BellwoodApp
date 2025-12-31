using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using System.Text.Json.Serialization;
using BellwoodGlobal.Core.Domain;
using BellwoodGlobal.Mobile.ViewModels;

namespace BellwoodGlobal.Mobile.Pages;

public partial class QuotePage : ContentPage
{
    private readonly IProfileService _profile;
    private readonly ObservableCollection<string> _additionalPassengers = new();
    private readonly IQuoteDraftBuilder _draftBuilder;
    private readonly IAdminApi _adminApi;
    private readonly ILocationPickerService _locationPicker;
    private readonly IFormStateService _formStateService; // NEW: Phase 5 form persistence

    // --- passenger/location UI constants (avoid string typos) ---
    private const string PassengerSelf = "Booker (you)";
    private const string PassengerNew = "New Passenger";
    private const string LocationNew = "New Location";
    private const string AsDirected = "As Directed";
    private bool _suppressPassengerCountEvents;

    // --- flight UI constants (avoid string typos) ---
    private const string FlightOptionTBD = "TBD";
    private const string FlightOptionCommercial = "Commercial Flight";
    private const string FlightOptionPrivate = "Private Tail Number";
    private const string ReqMeetAndGreet = "Meet & Greet";

    private List<Passenger> _savedPassengers = new();
    private List<Models.Location> _savedLocations = new();

    // NEW: Store selected locations from autocomplete (with coordinates)
    private Models.Location? _selectedPickupLocation;
    private Models.Location? _selectedDropoffLocation;

    // --- flight state for return logic ---
    private bool _allowReturnTailChange;     // private only
    private bool _requestsHasMeetOption;
    private bool _passengerCountDirty;
    
    // NEW: Phase 5 - Flag to prevent auto-save after successful submission
    private bool _submittedSuccessfully = false;

    // --- capacity evaluation ----
    private string? _suggestedVehicleClass;   // what we propose (e.g., "SUV")
    private bool _userChoseToKeep;            // set true if they pressed "Keep Current"

    // pax, checked, carry-on limits per class
    private readonly Dictionary<string, (int pax, int checkedBags, int carryOns)> _vehicleCaps = new()
    {
        { "Sedan",    (2, 2, 4) },
        { "S-Class",  (2, 2, 4) },
        { "SUV",      (4, 4, 8) },
        { "Sprinter", (8,10,20) },
    };

    private (bool within, string? note, string? suggestion) EvaluateCapacity(
        int pax, int checkedBags, int carryOns, string vehicleClass)
    {
        if (!_vehicleCaps.TryGetValue(vehicleClass, out var caps))
        {
            // Unknown class ? assume OK
            return (true, null, null);
        }

        var overPax = pax > caps.pax;
        var overCheck = checkedBags > caps.checkedBags;
        var overCarry = carryOns > caps.carryOns;

        if (!overPax && !overCheck && !overCarry)
            return (true, null, null);

        // Build a short note for JSON/email
        var reasons = new List<string>();
        if (overPax) reasons.Add($"pax {pax}/{caps.pax}");
        if (overCheck) reasons.Add($"checked {checkedBags}/{caps.checkedBags}");
        if (overCarry) reasons.Add($"carry-on {carryOns}/{caps.carryOns}");

        var suggestion = SuggestVehicle(pax, checkedBags, carryOns, vehicleClass);
        var note = $"Over capacity for {vehicleClass}: {string.Join(", ", reasons)}." +
                   (suggestion is not null ? $" Suggest {suggestion}." : "");

        return (false, note, suggestion);
    }

    private string? SuggestVehicle(int pax, int checkedBags, int carryOns, string current)
    {
        // try "next sizes up"
        var order = new[] { "Sedan", "S-Class", "SUV", "Sprinter" };

        // start from the next class above current, if possible
        var start = Math.Max(0, Array.IndexOf(order, current) + 1);

        for (int i = start; i < order.Length; i++)
        {
            var cls = order[i];
            if (!_vehicleCaps.TryGetValue(cls, out var caps)) continue;

            if (pax <= caps.pax && checkedBags <= caps.checkedBags && carryOns <= caps.carryOns)
                return cls;
        }
        return null; // nothing fits (rare)
    }

    private void RecomputeCapacityBanner()
    {
        var pax = (int)PassengerCountStepper.Value;
        var chk = (int)CheckedBagsStepper.Value;
        var carry = (int)CarryOnBagsStepper.Value;
        var cls = VehiclePicker.SelectedItem?.ToString() ?? "Sedan";

        var (within, note, suggestion) = EvaluateCapacity(pax, chk, carry, cls);

        _suggestedVehicleClass = suggestion;

        if (within || suggestion is null)
        {
            CapacityBanner.IsVisible = false;
            CapacityBannerText.Text = "";
            return;
        }

        // Respect prior "Keep Current" choice, but still show info
        var keepTag = _userChoseToKeep ? " (user chose to keep current vehicle)" : "";
        CapacityBannerText.Text = $"{note}{keepTag}";
        CapacityBanner.IsVisible = true;
    }

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
        _adminApi = ServiceHelper.GetRequiredService<IAdminApi>();
        _locationPicker = ServiceHelper.GetRequiredService<ILocationPickerService>();
        _formStateService = ServiceHelper.GetRequiredService<IFormStateService>(); // NEW: Phase 5

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
        VehiclePicker.SelectedIndexChanged += OnVehicleChanged;

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

        // Initialize capacity defaults
        _suppressPassengerCountEvents = true;
        PassengerCountStepper.Value = Math.Max(1, _additionalPassengers.Count + 1);
        PassengerCountValueLabel.Text = $"{(int)PassengerCountStepper.Value}";
        CheckedBagsValueLabel.Text = $"{(int)CheckedBagsStepper.Value}";
        CarryOnBagsValueLabel.Text = $"{(int)CarryOnBagsStepper.Value}";
        HoursValueLabel.Text = $"{(int)HoursStepper.Value}";
        _suppressPassengerCountEvents = false;

        // Keep return in sync with pickup and validate changes
        PickupDate.DateSelected += (_, __) => SyncReturnMinAndSuggest();
        PickupTime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) SyncReturnMinAndSuggest(); };

        ReturnDatePicker.DateSelected += (_, __) => EnsureReturnAfterPickup();
        ReturnTimePicker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) EnsureReturnAfterPickup(); };

        UpdateReturnFlightUx();
        UpdatePickupStyleAirportUx();
        UpdateReturnPickupStyleAirportUx();
        RecomputeCapacityBanner();
        ReevaluateCapacityAndMaybeShowBanner();
    }

    private void OnPassengerChanged(object? sender, EventArgs e)
    {
        var sel = PassengerPicker.SelectedItem?.ToString();

        if (sel == PassengerNew)
        {
            // Show edit grid + Save button for a brand new passenger
            PassengerNewGrid.IsVisible = true;
            SavePassengerButton.IsVisible = true;

            // Start blank for new entries
            PassengerFirst.Text = "";
            PassengerLast.Text = "";
            PassengerPhone.Text = "";
            PassengerEmail.Text = "";
            return;
        }

        // For existing/self passengers: show grid for viewing/editing,
        // but hide the Save button (not creating a contact here)
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
        SavePassengerButton.IsVisible = false;
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
        var pickupLoc = ResolveLocation(PickupLocationPicker, PickupNewLabel, PickupNewAddress);
        var isAirportPickup = IsAirportText(pickupLoc);

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
        System.Diagnostics.Debug.WriteLine($"[QuotePage] Pickup autocomplete selected: {location.Label} @ {location.Latitude}, {location.Longitude}");
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
        System.Diagnostics.Debug.WriteLine($"[QuotePage] Dropoff autocomplete selected: {location.Label} @ {location.Latitude}, {location.Longitude}");
#endif
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
        
        // Create location object, preserving coordinates if from autocomplete
        var loc = _selectedPickupLocation ?? new Models.Location { Label = label, Address = addr };
        
        // Update with current values (in case user edited after autocomplete)
        loc.Label = label;
        loc.Address = addr;
        
        _savedLocations.Add(loc);
        var display = loc.ToString();
        var insertAt = Math.Max(0, PickupLocationPicker.Items.Count - 1);
        PickupLocationPicker.Items.Insert(insertAt, display);
        PickupLocationPicker.SelectedIndex = insertAt;
        
        // Hide both autocomplete and manual entry grids
        PickupAutocompleteGrid.IsVisible = false;
        PickupNewGrid.IsVisible = false;
        
        await DisplayAlert("Saved", $"Pickup location added{(_selectedPickupLocation?.HasCoordinates == true ? " (with coordinates)" : "")}.", "OK");
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
        
        // Create location object, preserving coordinates if from autocomplete
        var loc = _selectedDropoffLocation ?? new Models.Location { Label = label, Address = addr };
        
        // Update with current values (in case user edited after autocomplete)
        loc.Label = label;
        loc.Address = addr;
        
        _savedLocations.Add(loc);
        var display = loc.ToString();
        var insertAt = Math.Max(1, DropoffPicker.Items.Count - 1); // after "As Directed"
        DropoffPicker.Items.Insert(insertAt, display);
        DropoffPicker.SelectedIndex = insertAt;
        
        // Hide both autocomplete and manual entry grids
        DropoffAutocompleteGrid.IsVisible = false;
        DropoffNewGrid.IsVisible = false;
        
        await DisplayAlert("Saved", $"Dropoff location added{(_selectedDropoffLocation?.HasCoordinates == true ? " (with coordinates)" : "")}.", "OK");
        UpdateReturnPickupStyleAirportUx();
    }

    // UPDATED: "Pick from Maps" becomes "View in Maps" (optional, view-only)
    private async void OnPickPickupFromMaps(object? sender, EventArgs e)
    {
        // If we have coordinates from autocomplete, open maps to that location
        if (_selectedPickupLocation?.HasCoordinates == true)
        {
            await _locationPicker.OpenInMapsAsync(_selectedPickupLocation);
            return;
        }
        
        // Otherwise, fallback to old behavior (pick from maps + manual entry)
        var result = await _locationPicker.PickLocationAsync(new LocationPickerOptions
        {
            Title = "Select Pickup Location",
            SuggestedLabel = (PickupNewLabel.Text ?? "").Trim(),
            InitialAddress = (PickupNewAddress.Text ?? "").Trim(),
            UseCurrentLocation = true
        });

        if (result.Success && result.Location is not null)
        {
            _selectedPickupLocation = result.Location;
            PickupNewLabel.Text = result.Location.Label;
            PickupNewAddress.Text = result.Location.Address;
            
#if DEBUG
            if (result.Location.HasCoordinates)
                System.Diagnostics.Debug.WriteLine($"[QuotePage] Pickup coordinates from maps: {result.Location.Latitude}, {result.Location.Longitude}");
#endif
        }
        else if (!result.WasCancelled && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            await DisplayAlert("Location Error", result.ErrorMessage, "OK");
        }
    }

    private async void OnPickDropoffFromMaps(object? sender, EventArgs e)
    {
        // If we have coordinates from autocomplete, open maps to that location
        if (_selectedDropoffLocation?.HasCoordinates == true)
        {
            await _locationPicker.OpenInMapsAsync(_selectedDropoffLocation);
            return;
        }
        
        // Otherwise, fallback to old behavior (pick from maps + manual entry)
        var result = await _locationPicker.PickLocationAsync(new LocationPickerOptions
        {
            Title = "Select Dropoff Location",
            SuggestedLabel = (DropoffNewLabel.Text ?? "").Trim(),
            InitialAddress = (DropoffNewAddress.Text ?? "").Trim(),
            UseCurrentLocation = false
        });

        if (result.Success && result.Location is not null)
        {
            _selectedDropoffLocation = result.Location;
            DropoffNewLabel.Text = result.Location.Label;
            DropoffNewAddress.Text = result.Location.Address;
            
#if DEBUG
            if (result.Location.HasCoordinates)
                System.Diagnostics.Debug.WriteLine($"[QuotePage] Dropoff coordinates from maps: {result.Location.Latitude}, {result.Location.Longitude}");
#endif
        }
        else if (!result.WasCancelled && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            await DisplayAlert("Location Error", result.ErrorMessage, "OK");
        }
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

        var (within, note, suggestion) = EvaluateCapacity(pax, chk, car, cls);
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

    private async void OnBuildJson(object? sender, EventArgs e)
    {
        // Basic required fields
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

        // Decide flight mode from picker
        var flightSel = FlightInfoPicker.SelectedItem?.ToString();
        var mode = flightSel == FlightOptionCommercial
            ? FlightMode.Commercial
            : flightSel == FlightOptionPrivate
                ? FlightMode.Private
                : FlightMode.None;

        // Enforce per-leg flight rules BEFORE building state
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

        // Build the state
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
            OutboundFlightNumber = (mode == FlightMode.Commercial) ? (FlightInfoEntry.Text ?? "").Trim() : null,
            OutboundTailNumber = (mode == FlightMode.Private) ? (FlightInfoEntry.Text ?? "").Trim() : null,
            AllowReturnTailChange = _allowReturnTailChange,
            ReturnFlightNumber = (mode == FlightMode.Commercial && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null,
            ReturnTailNumber = (mode == FlightMode.Private && retDT is not null) ? (ReturnFlightEntry.Text ?? "").Trim() : null
        };

        state.PassengerCount = (int)PassengerCountStepper.Value;
        state.CheckedBags = (int)CheckedBagsStepper.Value;
        state.CarryOnBags = (int)CarryOnBagsStepper.Value;

        // Compute capacity flags for json/email
        {
            var cls = state.VehicleClass ?? "Sedan";
            var (within, note, suggestion) = EvaluateCapacity(
                state.PassengerCount, state.CheckedBags ?? 0, state.CarryOnBags ?? 0, cls);

            state.CapacityWithinLimits = within;
            state.CapacityNote = note;
            state.SuggestedVehicle = suggestion;
            state.CapacityOverrideByUser = _userChoseToKeep;
        }

        var draft = _draftBuilder.Build(state);
        try
        {
            await _adminApi.SubmitQuoteAsync(draft);
            
            // NEW: Set flag to prevent auto-save and clear saved form state
            _submittedSuccessfully = true;
            await _formStateService.ClearQuoteFormStateAsync();
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[QuotePage] Form state cleared after successful submission");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", $"Could not submit to Admin: {ex.Message}", "OK");
            return; // Don't show JSON or clear state if submission failed
        }

        // Show JSON
        var json = JsonSerializer.Serialize(draft, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        JsonEditor.Text = json;
        JsonFrame.IsVisible = true;
        await DisplayAlert("Quote Ready", "The JSON has been built below.", "OK");
    }

    private async void OnCopyJson(object? sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(JsonEditor.Text ?? "");
        await DisplayAlert("Copied", "Quote JSON copied to clipboard.", "OK");
    }

    // ===== PHASE 5: FORM STATE PERSISTENCE =====

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check for saved form state
        if (_formStateService.HasSavedQuoteForm())
        {
            var shouldRestore = await DisplayAlert(
                "Restore Draft?",
                "You have an unsaved quote. Would you like to continue where you left off?",
                "Yes, Restore",
                "No, Start Fresh"
            );

            if (shouldRestore)
            {
                await RestoreFormStateAsync();
            }
            else
            {
                await _formStateService.ClearQuoteFormStateAsync();
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
            System.Diagnostics.Debug.WriteLine("[QuotePage] Skipping auto-save (form was successfully submitted)");
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
            var state = new QuotePageState
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

                LastModified = DateTime.UtcNow
            };

            await _formStateService.SaveQuoteFormStateAsync(state);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("[QuotePage] Form state saved");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuotePage] Error saving form state: {ex.Message}");
#endif
        }
    }

    private async Task RestoreFormStateAsync()
    {
        try
        {
            var state = await _formStateService.LoadQuoteFormStateAsync();
            if (state == null) return;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuotePage] Restoring form state from {state.LastModified}");
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

            // Restore Dropoff picker (handle As Directed and location indices)
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
                System.Diagnostics.Debug.WriteLine($"[QuotePage] Restored pickup coordinates: {_selectedPickupLocation.Latitude}, {_selectedPickupLocation.Longitude}");
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
                System.Diagnostics.Debug.WriteLine($"[QuotePage] Restored dropoff coordinates: {_selectedDropoffLocation.Latitude}, {_selectedDropoffLocation.Longitude}");
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

            await DisplayAlert("Draft Restored", "Your quote has been restored.", "OK");
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuotePage] Error restoring form state: {ex.Message}");
#endif
            await DisplayAlert("Restore Failed", "Could not restore your draft. Starting fresh.", "OK");
        }
    }

    private static string ResolveLocation(Picker picker, Entry label, Entry address)
    {
        var sel = picker.SelectedItem?.ToString();
        if (sel == LocationNew)
            return $"{(label.Text ?? "").Trim()} - {(address.Text ?? "").Trim()}".Trim(' ', '-');
        return sel ?? "";
    }
}
