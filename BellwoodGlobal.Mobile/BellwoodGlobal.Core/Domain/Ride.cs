namespace BellwoodGlobal.Core.Domain;

public class Ride
{
    public string Id { get; set; } = "";
    public DateTime PickupTime { get; set; }
    public string PickupAddress { get; set; } = "";
    public string DropoffAddress { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal? Price { get; set; }
}
