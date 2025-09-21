namespace BellwoodGlobal.Mobile.Models;

public class QuoteEstimate
{
    public string VehicleClass { get; set; } = "Sedan";
    public decimal EstimatedFare { get; set; }
    public TimeSpan EstimatedEta { get; set; }
}
