using System.Text.Json;
using System.Text.Json.Serialization;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using Microsoft.Maui.Controls;

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

#if DEBUG
        JsonCard.IsVisible = true;
        JsonEditor.Text = JsonSerializer.Serialize(d, _jsonOpts);
#endif
    }

    private static string EmptyAsNA(string? v) => string.IsNullOrWhiteSpace(v) ? "N/A" : v;

    // Same friendly mapping used on the dashboard
    private static readonly Dictionary<string, string> DisplayStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Submitted"] = "Submitted",
        ["InReview"] = "Pending",
        ["Priced"] = "Priced",
        ["Sent"] = "Quoted",
        ["Closed"] = "Closed",
        ["Rejected"] = "Declined"
    };

    private static string ToDisplayStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Submitted";
        return DisplayStatusMap.TryGetValue(raw, out var friendly) ? friendly : raw;
    }

    private static Color StatusColorForDisplay(string display) =>
        (display ?? "").ToLowerInvariant() switch
        {
            "submitted" or "pending" => (Color)Application.Current!.Resources["ChipPending"],
            "priced" or "quoted" => (Color)Application.Current!.Resources["ChipPriced"],
            "declined" => (Color)Application.Current!.Resources["ChipDeclined"],
            "closed" => (Color)Application.Current!.Resources["ChipOther"],
            _ => (Color)Application.Current!.Resources["ChipOther"]
        };

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
