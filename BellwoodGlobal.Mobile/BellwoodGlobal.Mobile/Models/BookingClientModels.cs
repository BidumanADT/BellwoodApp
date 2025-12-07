using System;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Models
{
    /// <summary>
    /// Lightweight booking list item (for dashboard).
    /// </summary>
    public sealed class BookingListItem
    {
        public string Id { get; set; } = "";
        public DateTime CreatedUtc { get; set; }
        public string Status { get; set; } = "Requested"; // Requested, Confirmed, Declined, Cancelled
        public string BookerName { get; set; } = "";
        public string PassengerName { get; set; } = "";
        public string VehicleClass { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string? DropoffLocation { get; set; }
        public DateTime PickupDateTime { get; set; }
        
        /// <summary>
        /// Assigned driver's internal ID (GUID-based for scalability).
        /// </summary>
        public string? AssignedDriverId { get; set; }
        
        /// <summary>
        /// Assigned driver name (null if unassigned).
        /// </summary>
        public string? AssignedDriverName { get; set; }
        
        /// <summary>
        /// Assigned driver's AuthServer identity (UserUid).
        /// Used for debugging driver assignment issues.
        /// </summary>
        public string? AssignedDriverUid { get; set; }
    }

    /// <summary>
    /// Full booking detail (includes draft).
    /// </summary>
    public sealed class BookingDetail
    {
        public string Id { get; set; } = "";
        public DateTime CreatedUtc { get; set; }
        public string Status { get; set; } = "Requested";
        public string BookerName { get; set; } = "";
        public string PassengerName { get; set; } = "";
        public string VehicleClass { get; set; } = "";
        public string PickupLocation { get; set; } = "";
        public string? DropoffLocation { get; set; }
        public DateTime PickupDateTime { get; set; }

        /// <summary>
        /// Assigned driver's internal ID (GUID-based for scalability).
        /// </summary>
        public string? AssignedDriverId { get; set; }

        /// <summary>
        /// Assigned driver name (null if unassigned).
        /// </summary>
        public string? AssignedDriverName { get; set; }
        
        /// <summary>
        /// Assigned driver's AuthServer identity (UserUid).
        /// Used for debugging driver assignment issues.
        /// </summary>
        public string? AssignedDriverUid { get; set; }

        public QuoteDraft Draft { get; set; } = new(); // Reuses QuoteDraft for trip details
    }
}