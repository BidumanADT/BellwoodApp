namespace BellwoodGlobal.Mobile.Models
{
    public enum FlightType
    {
        Commercial,
        Private
    }

    public sealed class FlightInfo
    {
        public FlightType Type { get; set; }
        public string? FlightNumber { get; set; } // commercial
        public string? TailNumber { get; set; }   // private
    }
}
