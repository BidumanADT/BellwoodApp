using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using BellwoodGlobal.Mobile.Services;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;
using BellwoodGlobal.Mobile.Helpers;

namespace BellwoodGlobal.Mobile.Pages;

public partial class BookingDetailPage : ContentPage, IQueryAttributable
{
    private readonly IAdminApi _admin;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? Id { get; private set; }

    // Store booking details for tracking navigation
    private Models.BookingDetail? _currentBooking;

    public BookingDetailPage()
    {
        InitializeComponent();
        _admin = ServiceHelper.GetRequiredService<IAdminApi>();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var v) || query.TryGetValue("Id", out v))
        {
            var incoming = v?.ToString();
            if (!string.IsNullOrWhiteSpace(incoming) && incoming != Id)
            {
                Id = incoming;
                await LoadAsync(Id!);
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrWhiteSpace(Id) && string.IsNullOrWhiteSpace(PassengerTitle.Text))
        {
            await LoadAsync(Id!);
        }
    }

    private async Task LoadAsync(string id)
    {
        try
        {
            var detail = await _admin.GetBookingAsync(id);
            if (detail is null)
            {
                await DisplayAlert("Not Found", "This booking could not be loaded.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            _currentBooking = detail;
            Bind(detail);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", $"Couldn't load booking: {ex.Message}", "OK");
        }
    }

    private void Bind(Models.BookingDetail d)
    {
        // Prefer CurrentRideStatus (driver-specific) over Status when available
        var effectiveStatus = !string.IsNullOrWhiteSpace(d.CurrentRideStatus) 
            ? d.CurrentRideStatus 
            : d.Status;
        
        // Status mapping (same as dashboard)
        string displayStatus = ToDisplayStatus(effectiveStatus);
        StatusChip.Text = displayStatus;
        StatusChipFrame.BackgroundColor = StatusColorForDisplay(displayStatus);
        PassengerTitle.Text = string.IsNullOrWhiteSpace(d.PassengerName) ? "Passenger" : d.PassengerName;

        SubHeader.Text =
            $"{(string.IsNullOrWhiteSpace(d.VehicleClass) ? "Vehicle" : d.VehicleClass)}  •  Created {DateTimeHelper.FormatForDisplay(d.CreatedUtc)}";

        // Show Track Driver banner when driver status is OnRoute/InProgress/Dispatched
        // Check CurrentRideStatus first (driver-specific), fallback to Status if not available
        var statusToCheck = !string.IsNullOrWhiteSpace(d.CurrentRideStatus) ? d.CurrentRideStatus : d.Status;
        var isTrackable = IsTrackableStatus(statusToCheck);
        TrackDriverBanner.IsVisible = isTrackable;

        // Pickup/Dropoff - Use DateTimeHelper to prevent double-conversion
        PickupLine.Text = $"{DateTimeHelper.FormatFriendly(d.PickupDateTime)} — {d.PickupLocation}";
        var draft = d.Draft ?? new QuoteDraft();

        // Pickup style/sign
        var pickupStyle = draft.PickupStyle == PickupStyle.MeetAndGreet ? "Meet & Greet" : "Curbside";
        PickupStyleLine.Text = $"Pickup Style: {pickupStyle}"
            + (pickupStyle == "Meet & Greet" && !string.IsNullOrWhiteSpace(draft.PickupSignText)
                ? $" — Sign: {draft.PickupSignText}"
                : "");

        DropoffLine.Text = string.IsNullOrWhiteSpace(d.DropoffLocation) ? "As Directed" : d.DropoffLocation;

        // Return (only if RoundTrip + ReturnPickupTime)
        var showReturn = draft.RoundTrip && draft.ReturnPickupTime is not null;
        ReturnCard.IsVisible = showReturn;
        if (showReturn)
        {
            ReturnPickupLine.Text = $"{draft.ReturnPickupTime.Value.ToLocalTime():g} — {(string.IsNullOrWhiteSpace(d.DropoffLocation) ? d.PickupLocation : d.DropoffLocation)}";

            var rtnStyle = draft.ReturnPickupStyle == PickupStyle.MeetAndGreet ? "Meet & Greet" : "Curbside";
            var rtnSign = (draft.ReturnPickupStyle == PickupStyle.MeetAndGreet && !string.IsNullOrWhiteSpace(draft.ReturnPickupSignText))
                ? $" — Sign: {draft.ReturnPickupSignText}" : "";
            ReturnStyleLine.Text = $"Return Pickup Style: {rtnStyle}{rtnSign}";

            ReturnDropoffLine.Text = $"Return Dropoff: {d.PickupLocation}";
        }

        // Flight
        var outbound = draft.OutboundFlight;
        var ret = draft.ReturnFlight;
        FlightCard.IsVisible = (outbound?.FlightNumber != null || outbound?.TailNumber != null || ret != null);
        if (FlightCard.IsVisible)
        {
            var obText = outbound is null
                ? "Outbound: N/A"
                : outbound.FlightNumber is not null
                    ? $"Outbound Flight #: {outbound.FlightNumber}"
                    : outbound.TailNumber is not null
                        ? $"Outbound Tail #: {outbound.TailNumber}"
                        : "Outbound: N/A";
            OutboundFlightLine.Text = obText;

            var rtText = ret is null
                ? ""
                : ret.FlightNumber is not null
                    ? $"Return Flight #: {ret.FlightNumber}"
                    : ret.TailNumber is not null
                        ? $"Return Tail #: {ret.TailNumber}"
                        : "";
            ReturnFlightLine.Text = rtText;
        }

        // Capacity + requests
        var pax = draft.PassengerCount;
        var checkedB = draft.CheckedBags ?? 0;
        var carryB = draft.CarryOnBags ?? 0;
        CapacityLine.Text = $"{pax} pax, {checkedB} checked, {carryB} carry-on";

        var capLines = new List<string>();
        if (draft.CapacityWithinLimits == false && !string.IsNullOrWhiteSpace(draft.CapacityNote))
            capLines.Add($"⚠ {draft.CapacityNote}");
        if (draft.CapacityOverrideByUser == true && !string.IsNullOrWhiteSpace(draft.SuggestedVehicle))
            capLines.Add($"Booker kept {d.VehicleClass} (suggested {draft.SuggestedVehicle}).");

        CapacityNoteLine.Text = string.Join(" ", capLines);

        // Show cancel button only for cancellable bookings
        var isCancellable = d.Status?.Equals("Requested", StringComparison.OrdinalIgnoreCase) == true ||
                            d.Status?.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) == true;
        CancelButton.IsVisible = isCancellable;

        var req = draft.AdditionalRequest;
        RequestLine.Text = string.IsNullOrWhiteSpace(req)
            ? "—"
            : req == "Other" && !string.IsNullOrWhiteSpace(draft.AdditionalRequestOtherText)
                ? $"Other — {draft.AdditionalRequestOtherText}"
                : req;

        // Booker / Passenger
        var booker = draft.Booker;
        var paxObj = draft.Passenger;
        BookerLine.Text = (booker is null) ? "—"
            : $"{booker} — {EmptyAsNA(booker.PhoneNumber)} — {EmptyAsNA(booker.EmailAddress)}";
        PaxLine.Text = (paxObj is null) ? "—"
            : $"{paxObj} — {EmptyAsNA(paxObj.PhoneNumber)} — {EmptyAsNA(paxObj.EmailAddress)}";

#if DEBUG
        JsonCard.IsVisible = true;
        JsonEditor.Text = JsonSerializer.Serialize(d, _jsonOpts);
#endif
    }

    /// <summary>
    /// Determines if the ride status allows driver tracking.
    /// </summary>
    private static bool IsTrackableStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        return status.Equals("OnRoute", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("EnRoute", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Arrived", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("PassengerOnboard", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Navigate to the Driver Tracking page.
    /// </summary>
    private async void OnTrackDriverClicked(object? sender, EventArgs e)
    {
        if (_currentBooking == null || string.IsNullOrWhiteSpace(Id))
        {
            await DisplayAlert("Error", "Booking details not available.", "OK");
            return;
        }

        var draft = _currentBooking.Draft;

        // Get pickup coordinates from the draft if available
        // Default to NYC coordinates if not available (fallback for demo)
        double pickupLat = draft?.PickupLatitude ?? 40.7128;
        double pickupLng = draft?.PickupLongitude ?? -74.0060;

        var pickupAddress = Uri.EscapeDataString(_currentBooking.PickupLocation ?? "Pickup");

        // Navigate to tracking page with parameters
        var route = $"{nameof(DriverTrackingPage)}?rideId={Uri.EscapeDataString(Id)}&pickupLat={pickupLat}&pickupLng={pickupLng}&pickupAddress={pickupAddress}";

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[BookingDetailPage] Navigating to tracking: {route}");
#endif

        await Shell.Current.GoToAsync(route);
    }

    // Cancel button handler
    private async void OnCancelBookingClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            await DisplayAlert("Error", "Booking ID not found.", "OK");
            return;
        }

        // Confirm cancellation
        var confirm = await DisplayAlert(
            "Cancel Booking",
            "Are you sure you want to cancel this booking? Bellwood staff will be notified immediately.",
            "Yes, Cancel",
            "No, Keep Booking");

        if (!confirm) return;

        try
        {
            // Disable button during API call
            CancelButton.IsEnabled = false;
            CancelButton.Text = "Cancelling...";

            await _admin.CancelBookingAsync(Id!);

            await DisplayAlert("Cancelled", "Your booking has been cancelled. Bellwood staff have been notified.", "OK");

            // Navigate back to bookings list
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not cancel booking: {ex.Message}", "OK");

            // Re-enable button on failure
            CancelButton.IsEnabled = true;
            CancelButton.Text = "Cancel Booking";
        }
    }

    private static string EmptyAsNA(string? v) => string.IsNullOrWhiteSpace(v) ? "N/A" : v;

    // Same friendly mapping used on the dashboard
    private static readonly Dictionary<string, string> DisplayStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Requested"] = "Requested",
        ["Confirmed"] = "Confirmed",
        ["Scheduled"] = "Scheduled",
        ["OnRoute"] = "Driver En Route",
        ["InProgress"] = "In Progress",
        ["Dispatched"] = "Dispatched",
        ["EnRoute"] = "En Route",
        ["Arrived"] = "Driver Arrived",
        ["PassengerOnboard"] = "Passenger On Board",
        ["Completed"] = "Completed",
        ["Cancelled"] = "Cancelled",
        ["NoShow"] = "No Show"
    };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Requested";
        return DisplayStatusMap.TryGetValue(raw, out var friendly) ? friendly : raw;
    }

    private static Color StatusColorForDisplay(string display) =>
        (display ?? "").ToLowerInvariant() switch
        {
            "requested" => TryGetColor("ChipPending", Colors.Goldenrod),
            "confirmed" or "scheduled" => TryGetColor("ChipPriced", Colors.SeaGreen),
            "driver en route" or "dispatched" or "en route" => TryGetColor("BellwoodGold", Colors.Gold),
            "driver arrived" or "arrived" => TryGetColor("BellwoodGold", Colors.Gold),
            "passenger on board" or "passengeronboard" => TryGetColor("BellwoodGold", Colors.Gold),
            "in progress" => TryGetColor("BellwoodGold", Colors.Gold),
            "completed" => TryGetColor("ChipOther", Colors.LightGray),
            "cancelled" or "no show" => TryGetColor("ChipDeclined", Colors.IndianRed),
            _ => TryGetColor("ChipOther", Colors.Gray)
        };

    private static Color TryGetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
