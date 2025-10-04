using System.Linq;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class QuoteDraftBuilder : IQuoteDraftBuilder
    {
        public QuoteDraft Build(QuoteFormState s)
        {
            var draft = new QuoteDraft
            {
                Booker = s.Booker,
                Passenger = s.Passenger,
                AdditionalPassengers = s.AdditionalPassengers?.ToList() ?? new(),

                VehicleClass = s.VehicleClass ?? "Sedan",

                PickupDateTime = s.PickupDateTime,
                PickupLocation = s.PickupLocation ?? "",

                AsDirected = s.AsDirected,
                Hours = s.AsDirected ? s.Hours : null,
                DropoffLocation = s.AsDirected ? null : s.DropoffLocation,

                RoundTrip = !s.AsDirected && s.RoundTrip,
                ReturnPickupTime = (!s.AsDirected && s.RoundTrip) ? s.ReturnPickupTime : null,

                AdditionalRequest = s.AdditionalRequest,
                AdditionalRequestOtherText = s.AdditionalRequestOtherText,

                OutboundFlight = BuildOutboundFlight(s),
                ReturnFlight = BuildReturnFlight(s)
            };

            return draft;
        }

        private static FlightInfo BuildOutboundFlight(QuoteFormState s)
        {
            switch (s.FlightMode)
            {
                case FlightMode.Commercial:
                    return new FlightInfo { FlightNumber = string.IsNullOrWhiteSpace(s.OutboundFlightNumber) ? null : s.OutboundFlightNumber };
                case FlightMode.Private:
                    return new FlightInfo { TailNumber = string.IsNullOrWhiteSpace(s.OutboundTailNumber) ? null : s.OutboundTailNumber };
                default:
                    // model requires a non-null OutboundFlight; keep empty when no flight info
                    return new FlightInfo();
            }
        }

        private static FlightInfo? BuildReturnFlight(QuoteFormState s)
        {
            if (s.AsDirected || !s.RoundTrip || s.ReturnPickupTime is null)
                return null;

            switch (s.FlightMode)
            {
                case FlightMode.Commercial:
                    // For commercial round trips, we expect a *new* return flight number.
                    return string.IsNullOrWhiteSpace(s.ReturnFlightNumber)
                        ? null
                        : new FlightInfo { FlightNumber = s.ReturnFlightNumber };

                case FlightMode.Private:
                    // If not changing aircraft, reuse outbound tail. If changing, require a return tail.
                    var tail = s.AllowReturnTailChange
                        ? (string.IsNullOrWhiteSpace(s.ReturnTailNumber) ? s.OutboundTailNumber : s.ReturnTailNumber)
                        : s.OutboundTailNumber;

                    return string.IsNullOrWhiteSpace(tail) ? null : new FlightInfo { TailNumber = tail };

                default:
                    return null;
            }
        }
    }
}
