using System;

namespace BellwoodGlobal.Core.Models
{
    // possible life-cycle states of a quote
    public enum QuoteStatus
    {
        Pending,
        Accepted,
        Expired,
        Rejected
    }

    public class Quote
    {
        public int Id { get; set; }

        // who requested this quote
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        // once booked, you can link back to the reservation
        // (null until they actually reserve)
        public int? ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        // pricing info
        public decimal EstimatedCost { get; set; }
        public string Currency { get; set; } = "USD";

        // route details
        public double DistanceMiles { get; set; }
        public TimeSpan EstimatedDuration { get; set; }

        // when this quote was created and when it expires
        public DateTime CreatedAt { get; set; }
        public DateTime ValidUntil { get; set; }

        public QuoteStatus Status { get; set; }
    }
}
