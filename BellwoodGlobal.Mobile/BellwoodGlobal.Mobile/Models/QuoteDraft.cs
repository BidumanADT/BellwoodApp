namespace BellwoodGlobal.Mobile.Models;

public class QuoteDraft
{
    public Passenger Booker { get; set; } = new();
    public Passenger Passenger { get; set; } = new();
    public List<string> AdditionalPassengers { get; set; } = new();
    public string VehicleClass { get; set; } = "Sedan";

    public DateTime PickupDateTime { get; set; }
    public string PickupLocation { get; set; } = "";

    public bool AsDirected { get; set; }
    public int? Hours { get; set; } // As Directed only

    public string? DropoffLocation { get; set; } // null if As Directed
    public bool RoundTrip { get; set; }
    public DateTime? ReturnPickupTime { get; set; }

    public string? AdditionalRequest { get; set; }
    public string? AdditionalRequestOtherText { get; set; }
}
