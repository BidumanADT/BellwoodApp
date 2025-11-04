namespace BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


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
}

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
}
