using System;
using System.Collections.Generic;

namespace BellwoodGlobal.Mobile.Models
{
    public enum FlightMode
    {
        None,
        Commercial,
        Private
    }

    public sealed class QuoteFormState
    {
        // Core people
        public Passenger Booker { get; set; } = new();
        public Passenger Passenger { get; set; } = new();
        public List<string> AdditionalPassengers { get; set; } = new();

        // Trip details
        public string VehicleClass { get; set; } = "Sedan";
        public DateTime PickupDateTime { get; set; }
        public string PickupLocation { get; set; } = "";
        public bool AsDirected { get; set; }
        public int? Hours { get; set; }
        public string? DropoffLocation { get; set; }

        // Return / round trip
        public bool RoundTrip { get; set; }
        public DateTime? ReturnPickupTime { get; set; }

        // Requests
        public string? AdditionalRequest { get; set; }
        public string? AdditionalRequestOtherText { get; set; }

        // Flight selection
        public FlightMode FlightMode { get; set; } = FlightMode.None;

        // Outbound flight inputs
        public string? OutboundFlightNumber { get; set; } // commercial
        public string? OutboundTailNumber { get; set; }   // private

        // Return flight inputs
        public bool AllowReturnTailChange { get; set; }   // private
        public string? ReturnFlightNumber { get; set; }   // commercial
        public string? ReturnTailNumber { get; set; }     // private
    }
}
