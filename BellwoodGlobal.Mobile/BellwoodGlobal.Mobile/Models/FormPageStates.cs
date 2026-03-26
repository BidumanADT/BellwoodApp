using System;
using System.Collections.Generic;

namespace BellwoodGlobal.Mobile.Models
{
    /// <summary>
    /// Represents the persisted UI state of the Quote form for lifecycle resilience.
    /// Different from TripFormState (which is for submission) - this captures ALL UI state including in-progress edits.
    /// Stored in MAUI Preferences as JSON.
    /// </summary>
    public class QuotePageState
    {
        // ===== Pickup Location =====
        public int? PickupLocationIndex { get; set; }
        public string? PickupNewLabel { get; set; }
        public string? PickupNewAddress { get; set; }
        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
        public string? PickupPlaceId { get; set; }
        
        // ===== Dropoff Location =====
        public string? DropoffSelection { get; set; } // Picker item text: "As Directed" | "New Location" | saved location name
        public int? DropoffLocationIndex { get; set; }
        public string? DropoffNewLabel { get; set; }
        public string? DropoffNewAddress { get; set; }
        public double? DropoffLatitude { get; set; }
        public double? DropoffLongitude { get; set; }
        public string? DropoffPlaceId { get; set; }
        
        // ===== Date/Time =====
        public DateTime? PickupDate { get; set; }
        public TimeSpan? PickupTime { get; set; }
        public DateTime? ReturnDate { get; set; }
        public TimeSpan? ReturnTime { get; set; }
        
        // ===== Vehicle & Passenger =====
        public int? VehiclePickerIndex { get; set; }
        public int? PassengerPickerIndex { get; set; }
        public string? PassengerFirstName { get; set; }
        public string? PassengerLastName { get; set; }
        public string? PassengerPhone { get; set; }
        public string? PassengerEmail { get; set; }
        
        // ===== Additional Passengers =====
        public List<string> AdditionalPassengers { get; set; } = new();
        
        // ===== Round Trip =====
        public bool RoundTrip { get; set; }
        
        // ===== Luggage =====
        public int PassengerCount { get; set; }
        public int CheckedBags { get; set; }
        public int CarryOnBags { get; set; }
        
        // ===== As Directed =====
        public bool AsDirected { get; set; }
        public int Hours { get; set; } = 2;
        
        // ===== Flight Info =====
        public int? FlightInfoPickerIndex { get; set; } // 0=TBD, 1=Commercial, 2=Private
        public string? FlightInfoEntry { get; set; }
        public string? ReturnFlightEntry { get; set; }
        public bool AllowReturnTailChange { get; set; }
        
        // ===== Pickup Style & Sign (Airport) =====
        public int? PickupStylePickerIndex { get; set; }
        public string? PickupSignEntry { get; set; }
        public int? ReturnPickupStylePickerIndex { get; set; }
        public string? ReturnPickupSignEntry { get; set; }
        
        // ===== Additional Requests =====
        public int? RequestsPickerIndex { get; set; }
        public string? RequestOtherEntry { get; set; }
        public string? NonAirportMeetSignEntry { get; set; }
        
        // ===== Autocomplete In-Progress Text =====
        public string? AutocompleteSearchText_Pickup { get; set; }
        public string? AutocompleteSearchText_Dropoff { get; set; }
        
        // ===== Metadata =====
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents the persisted UI state of the Book Ride form.
    /// Extends QuotePageState with payment-specific fields.
    /// </summary>
    public class BookRidePageState : QuotePageState
    {
        // ===== Payment =====
        public int? PaymentPickerIndex { get; set; }
        public string? NewCardHolderName { get; set; }
        // NOTE: Never persist full card number or CVC!
    }
}
