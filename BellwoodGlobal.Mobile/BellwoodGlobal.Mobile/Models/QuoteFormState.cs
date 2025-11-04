using System;
using System.Collections.Generic;
using BellwoodGlobal.Core.Domain;

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
        public PickupStyle PickupStyle { get; set; } = PickupStyle.Curbside;
        public string? PickupSignText { get; set; }
        public PickupStyle? ReturnPickupStyle { get; set; }
        public string? ReturnPickupSignText { get; set; }
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
        
        // capacity inputs
        public int PassengerCount { get; set; }
        public int? CheckedBags { get; set; }
        public int? CarryOnBags { get; set; }

        // capacity evaluation outcome
        public bool CapacityWithinLimits { get; set; }  // computed in UI before building draft
        public string? CapacityNote { get; set; }       // e.g., "User kept Sedan; suggested SUV"
        public string? SuggestedVehicle { get; set; } // based on capacity
        public bool CapacityOverrideByUser { get; set; } // true if user pressed "Keep Current"
    }
}
