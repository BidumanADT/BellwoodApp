using System.Text.Json.Serialization;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Models.Places;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile;

/// <summary>
/// Source-generated JSON serializer context.
/// Prevents the IL trimmer from stripping property metadata
/// on model types that are deserialized from JSON at runtime.
/// Without this, Release builds crash because System.Text.Json
/// reflection loses access to trimmed property setters.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]

// AdminApi response models
[JsonSerializable(typeof(BookerProfile))]
[JsonSerializable(typeof(List<QuoteListItem>))]
[JsonSerializable(typeof(QuoteDetail))]
[JsonSerializable(typeof(AcceptQuoteResponse))]
[JsonSerializable(typeof(CancelQuoteResponse))]
[JsonSerializable(typeof(QuoteErrorResponse))]
[JsonSerializable(typeof(List<BookingListItem>))]
[JsonSerializable(typeof(BellwoodGlobal.Mobile.Models.BookingDetail))]
[JsonSerializable(typeof(List<SavedPassengerDto>))]
[JsonSerializable(typeof(SavedPassengerDto))]
[JsonSerializable(typeof(List<SavedLocationDto>))]
[JsonSerializable(typeof(SavedLocationDto))]
[JsonSerializable(typeof(SavePassengerRequest))]
[JsonSerializable(typeof(SaveLocationRequest))]
[JsonSerializable(typeof(PaymentMethod))]
[JsonSerializable(typeof(List<PaymentMethod>))]
[JsonSerializable(typeof(NewCardRequest))]

// Driver tracking
[JsonSerializable(typeof(DriverLocation))]
[JsonSerializable(typeof(PassengerLocationResponse))]

// Google Places API models
[JsonSerializable(typeof(AutocompleteResponse))]
[JsonSerializable(typeof(PlaceDetails))]

// Domain models (serialized in quote/booking submission)
[JsonSerializable(typeof(QuoteDraft))]
[JsonSerializable(typeof(Passenger))]
[JsonSerializable(typeof(List<Passenger>))]
[JsonSerializable(typeof(FlightInfo))]
[JsonSerializable(typeof(BellwoodGlobal.Core.Domain.BookingDetail))]

// Location model (cached to Preferences by ProfileService)
[JsonSerializable(typeof(BellwoodGlobal.Mobile.Models.Location))]
[JsonSerializable(typeof(List<BellwoodGlobal.Mobile.Models.Location>))]

// Form state persistence (serialized to Preferences)
[JsonSerializable(typeof(QuotePageState))]
[JsonSerializable(typeof(BookRidePageState))]

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class BellwoodJsonContext : JsonSerializerContext
{
}
