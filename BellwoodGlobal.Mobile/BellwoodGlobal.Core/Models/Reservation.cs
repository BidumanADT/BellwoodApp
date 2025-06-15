using System;

namespace BellwoodGlobal.Core.Models
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }

    public class Reservation
    {
        public int Id { get; set; }

        // link to the customer who booked
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        // ride details
        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }

        public ReservationStatus Status { get; set; }

        // quotes
        public ICollection<Quote> Quotes { get; set; }

        // audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
