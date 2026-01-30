using System.Text.Json;
using System.Text.Json.Serialization;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using Microsoft.Maui.Controls;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Pages;

public partial class QuoteDetailPage : ContentPage, IQueryAttributable
{
    private readonly IAdminApi _admin;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? Id { get; private set; }

    public QuoteDetailPage()
    {
        InitializeComponent();
        _admin = ServiceHelper.GetRequiredService<IAdminApi>();
    }

    // MAUI Shell will call this when navigating with route parameters
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Accept either "id" or "Id"
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

    // Optional: safety net if the page gets re-shown with an Id already set
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
            var detail = await _admin.GetQuoteAsync(id);
            if (detail is null)
            {
                await DisplayAlert("Not Found", "This quote could not be loaded.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
            Bind(detail);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Network", $"Couldn't load quote: {ex.Message}", "OK");
        }
    }

    private void Bind(QuoteDetail d)
    {
        // Status mapping (same as dashboard)
        string displayStatus = ToDisplayStatus(d.Status);
        StatusChip.Text = displayStatus;
        StatusChipFrame.BackgroundColor = StatusColorForDisplay(displayStatus);
        PassengerTitle.Text = string.IsNullOrWhiteSpace(d.PassengerName) ? "Passenger" : d.PassengerName;

        SubHeader.Text =
            $"{(string.IsNullOrWhiteSpace(d.VehicleClass) ? "Vehicle" : d.VehicleClass)}  •  Created {d.CreatedUtc.ToLocalTime():g}";

        // Pickup/Dropoff
        PickupLine.Text = $"{d.PickupDateTime.ToLocalTime():g} — {d.PickupLocation}";
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

            // Return dropoff is typically the original pickup (nice to show explicitly)
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

        // Phase Alpha: Dispatcher Response Section
        var showResponse = displayStatus is "Response Received" or "Booking Created";
        ResponseCard.IsVisible = showResponse;
        
        if (showResponse && d.EstimatedPrice.HasValue)
        {
            EstimatedPriceLabel.Text = $"${d.EstimatedPrice.Value:F2}";
            
            if (d.EstimatedPickupTime.HasValue)
            {
                EstimatedPickupLabel.Text = d.EstimatedPickupTime.Value.ToLocalTime().ToString("MMM dd, yyyy @ h:mm tt");
            }
            else
            {
                EstimatedPickupLabel.Text = "Not specified";
            }
            
            if (!string.IsNullOrWhiteSpace(d.Notes))
            {
                NotesSection.IsVisible = true;
                NotesLabel.Text = d.Notes;
            }
            else
            {
                NotesSection.IsVisible = false;
            }
            
            // Show status-specific message
            if (displayStatus == "Response Received")
            {
                ResponseMessage.IsVisible = true;
                ResponseMessage.Text = "We've prepared an estimate for your trip!";
            }
            else if (displayStatus == "Booking Created")
            {
                ResponseMessage.IsVisible = true;
                ResponseMessage.Text = "This quote has been accepted. Your booking is ready!";
            }
        }

        // Phase Alpha: Action Buttons (dynamic based on status)
        UpdateActionButtons(displayStatus);

#if DEBUG
        JsonCard.IsVisible = true;
        JsonEditor.Text = JsonSerializer.Serialize(d, _jsonOpts);
#endif
    }

    private void UpdateActionButtons(string displayStatus)
    {
        // Reset all buttons
        AcceptButton.IsVisible = false;
        CancelButton.IsVisible = false;
        ViewBookingButton.IsVisible = false;
        ActionButtonsSection.IsVisible = false;

        switch (displayStatus)
        {
            case "Awaiting Response":
            case "Under Review":
                // Only show Cancel button
                CancelButton.IsVisible = true;
                ActionButtonsSection.IsVisible = true;
                break;

            case "Response Received":
                // Show both Accept and Cancel buttons
                AcceptButton.IsVisible = true;
                CancelButton.IsVisible = true;
                ActionButtonsSection.IsVisible = true;
                break;

            case "Booking Created":
                // Show View Booking button
                ViewBookingButton.IsVisible = true;
                ActionButtonsSection.IsVisible = true;
                break;

            case "Cancelled":
                // No buttons (read-only)
                ActionButtonsSection.IsVisible = false;
                break;
        }
    }

    // Phase Alpha: Button Event Handlers (placeholders for next commit)
    private async void OnAcceptQuoteClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            await DisplayAlert("Error", "Quote ID is missing.", "OK");
            return;
        }

        // Disable button to prevent double-clicks
        AcceptButton.IsEnabled = false;

        try
        {
            // Call API to accept quote
            var result = await _admin.AcceptQuoteAsync(Id);

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[QuoteDetail] Quote {Id} accepted. Booking created: {result.BookingId}");
#endif

            // Show success message
            var navigateToBooking = await DisplayAlert(
                "Success!",
                "Quote accepted! Your booking has been created.",
                "View Booking",
                "OK");

            if (navigateToBooking && !string.IsNullOrWhiteSpace(result.BookingId))
            {
                // Navigate to booking detail page
                await Shell.Current.GoToAsync($"{nameof(BookingDetailPage)}?id={result.BookingId}");
            }
            else
            {
                // Go back to quote dashboard
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., wrong status)
            await DisplayAlert(
                "Cannot Accept Quote",
                ex.Message,
                "OK");

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuoteDetail] Accept failed (business rule): {ex.Message}");
#endif

            // Reload quote to get current status
            await LoadAsync(Id);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied (shouldn't happen for own quotes)
            await DisplayAlert(
                "Access Denied",
                "You don't have permission to accept this quote.",
                "OK");

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuoteDetail] Accept failed (unauthorized): {ex.Message}");
#endif
        }
        catch (Exception ex)
        {
            // Generic error
            await DisplayAlert(
                "Error",
                $"Failed to accept quote: {ex.Message}",
                "OK");

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[QuoteDetail] Accept failed (error): {ex.Message}");
#endif
        }
        finally
        {
            // Re-enable button
            AcceptButton.IsEnabled = true;
        }
    }

    private async void OnCancelQuoteClicked(object? sender, EventArgs e)
    {
        // TODO: Implement cancel quote logic in next commit
        await DisplayAlert("Coming Soon", "Cancel quote functionality will be implemented in the next step.", "OK");
    }

    private async void OnViewBookingClicked(object? sender, EventArgs e)
    {
        // TODO: Navigate to booking detail page
        await DisplayAlert("Coming Soon", "Navigate to booking detail will be implemented in the next step.", "OK");
    }

    private static string EmptyAsNA(string? v) => string.IsNullOrWhiteSpace(v) ? "N/A" : v;

    // Same friendly mapping used on the dashboard (Phase Alpha + backward compatibility)
    private static readonly Dictionary<string, string> DisplayStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Phase Alpha statuses (new)
        ["Pending"] = "Awaiting Response",
        ["Acknowledged"] = "Under Review",
        ["Responded"] = "Response Received",
        ["Accepted"] = "Booking Created",
        ["Cancelled"] = "Cancelled",
        
        // Legacy statuses (backward compatibility)
        ["Submitted"] = "Awaiting Response",
        ["InReview"] = "Under Review",
        ["Priced"] = "Response Received",
        ["Sent"] = "Response Received",
        ["Closed"] = "Booking Created",
        ["Rejected"] = "Cancelled"
    };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Awaiting Response";
        return DisplayStatusMap.TryGetValue(raw, out var friendly) ? friendly : raw;
    }

    private static Color StatusColorForDisplay(string display) =>
        (display ?? "").ToLowerInvariant() switch
        {
            "awaiting response" => Colors.Orange,
            "under review" => Colors.Blue,
            "response received" => Colors.Green,
            "booking created" => Colors.Gray,
            "cancelled" => Colors.Red,
            
            // Legacy fallbacks
            "submitted" or "pending" => Colors.Orange,
            "priced" or "quoted" => Colors.Green,
            "declined" => Colors.Red,
            "closed" => Colors.Gray,
            
            _ => Colors.Gray
        };

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
