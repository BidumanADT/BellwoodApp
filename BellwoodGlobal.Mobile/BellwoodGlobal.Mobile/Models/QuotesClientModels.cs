namespace BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


/// <summary>
/// Quote list item for Quote Dashboard display.
/// Contains summary information and new lifecycle fields for Phase Alpha.
/// </summary>
public sealed class QuoteListItem
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string Status { get; set; } = "Submitted";
    public string BookerName { get; set; } = "";
    public string PassengerName { get; set; } = "";
    public string VehicleClass { get; set; } = "";
    public string PickupLocation { get; set; } = "";
    public string? DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }

    // Phase Alpha: Lifecycle fields for quick status display
    public decimal? EstimatedPrice { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// Full quote details for Quote Detail page.
/// Includes all lifecycle fields and dispatcher response data.
/// </summary>
public sealed class QuoteDetail
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string Status { get; set; } = "Submitted";
    public string BookerName { get; set; } = "";
    public string PassengerName { get; set; } = "";
    public string VehicleClass { get; set; } = "";
    public string PickupLocation { get; set; } = "";
    public string? DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }

    public QuoteDraft Draft { get; set; } = new();

    // Phase Alpha: Lifecycle tracking fields
    public string? CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }

    // Phase Alpha: Acknowledgement tracking
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByUserId { get; set; }

    // Phase Alpha: Response tracking
    public DateTime? RespondedAt { get; set; }
    public string? RespondedByUserId { get; set; }

    // Phase Alpha: Dispatcher response data
    public decimal? EstimatedPrice { get; set; }
    public DateTime? EstimatedPickupTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Response from accept quote endpoint.
/// Contains new booking ID for navigation.
/// </summary>
public sealed class AcceptQuoteResponse
{
    public string Message { get; set; } = "";
    public string QuoteId { get; set; } = "";
    public string QuoteStatus { get; set; } = "";
    public string BookingId { get; set; } = "";
    public string BookingStatus { get; set; } = "";
    public string? SourceQuoteId { get; set; }
}

/// <summary>
/// Response from cancel quote endpoint.
/// </summary>
public sealed class CancelQuoteResponse
{
    public string Message { get; set; } = "";
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
}

/// <summary>
/// Generic error response from AdminAPI.
/// </summary>
public sealed class QuoteErrorResponse
{
    public string? Error { get; set; }
}
