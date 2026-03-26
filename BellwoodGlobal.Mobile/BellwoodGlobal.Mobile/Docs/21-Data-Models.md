# Data Models & Entities

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document describes all data models, DTOs (Data Transfer Objects), and entities used in the Bellwood Global Mobile App. It covers client-side models, API request/response models, and domain entities.

**Model Categories**:
- ?? **Domain Entities** - Core business objects (shared with backend)
- ?? **Request DTOs** - Data sent to AdminAPI
- ?? **Response DTOs** - Data received from AdminAPI
- ?? **Client Models** - Mobile-specific models

---

## ?? Domain Entities

### QuoteDraft

**Location**: `BellwoodGlobal.Core/Domain/QuoteDraft.cs`

**Purpose**: Quote request data (submitted by passengers)

**Schema**:
```csharp
public sealed class QuoteDraft
{
    // Booker Information
    public ContactInfo Booker { get; set; }
    
    // Passenger Information (may differ from booker)
    public ContactInfo Passenger { get; set; }
    
    // Trip Details
    public string VehicleClass { get; set; }           // "Sedan", "SUV", "Executive Sedan", etc.
    public DateTime PickupDateTime { get; set; }       // Requested pickup time (UTC)
    public string PickupLocation { get; set; }         // Pickup address
    public double? PickupLatitude { get; set; }        // GPS coordinates (from Google Places)
    public double? PickupLongitude { get; set; }
    public string PickupStyle { get; set; }            // "Curbside", "MeetAndGreet"
    
    public string DropoffLocation { get; set; }        // Destination address
    public double? DropoffLatitude { get; set; }       // GPS coordinates (from Google Places)
    public double? DropoffLongitude { get; set; }
    
    // Trip Options
    public bool RoundTrip { get; set; }                // Is this a round trip?
    public DateTime? ReturnPickupDateTime { get; set; } // Return trip time (if round trip)
    public string ReturnPickupLocation { get; set; }   // Return pickup location
    public string ReturnPickupStyle { get; set; }
    public string ReturnDropoffLocation { get; set; }
    
    // Passengers & Luggage
    public int PassengerCount { get; set; }            // Number of passengers
    public int CheckedBags { get; set; }               // Checked luggage count
    public int CarryOnBags { get; set; }               // Carry-on luggage count
    
    // Flight Information (optional)
    public FlightInfo? FlightInfo { get; set; }
    
    // Additional Details
    public string AdditionalRequest { get; set; }      // "None" or special requests
}
```

**Field Descriptions**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `Booker` | ContactInfo | Yes | Person requesting the quote |
| `Passenger` | ContactInfo | Yes | Person traveling (may be same as booker) |
| `VehicleClass` | string | Yes | Vehicle type (see VehicleClass enum) |
| `PickupDateTime` | DateTime | Yes | Requested pickup time (UTC) |
| `PickupLocation` | string | Yes | Pickup address |
| `PickupLatitude` | double? | No | GPS latitude (recommended) |
| `PickupLongitude` | double? | No | GPS longitude (recommended) |
| `PickupStyle` | string | Yes | "Curbside" or "MeetAndGreet" |
| `DropoffLocation` | string | Yes | Destination address |
| `RoundTrip` | bool | No | Default: false |
| `PassengerCount` | int | Yes | Must be ? 1 |
| `CheckedBags` | int | No | Default: 0 |
| `CarryOnBags` | int | No | Default: 0 |

**Usage**:
```csharp
var quoteDraft = new QuoteDraft
{
    Booker = new ContactInfo
    {
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "312-555-0001",
        EmailAddress = "john.doe@example.com"
    },
    Passenger = new ContactInfo
    {
        FirstName = "Jane",
        LastName = "Smith",
        PhoneNumber = "312-555-0100",
        EmailAddress = "jane.smith@example.com"
    },
    VehicleClass = "Sedan",
    PickupDateTime = DateTime.Parse("2026-02-15T14:30:00Z"),
    PickupLocation = "O'Hare Airport Terminal 1",
    PickupLatitude = 41.9742,
    PickupLongitude = -87.9073,
    PickupStyle = "Curbside",
    DropoffLocation = "Downtown Chicago, 100 N LaSalle St",
    DropoffLatitude = 41.8843,
    DropoffLongitude = -87.6324,
    RoundTrip = false,
    PassengerCount = 2,
    CheckedBags = 2,
    CarryOnBags = 1,
    AdditionalRequest = "None"
};
```

---

### ContactInfo

**Location**: `BellwoodGlobal.Core/Domain/ContactInfo.cs`

**Purpose**: Person contact details (booker, passenger)

**Schema**:
```csharp
public sealed class ContactInfo
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string EmailAddress { get; set; }
}
```

**Field Descriptions**:

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `FirstName` | string | Yes | Not empty |
| `LastName` | string | Yes | Not empty |
| `PhoneNumber` | string | Yes | Valid phone format |
| `EmailAddress` | string | Yes | Valid email format |

---

### FlightInfo

**Location**: `BellwoodGlobal.Core/Domain/FlightInfo.cs`

**Purpose**: Flight details for airport pickups/dropoffs

**Schema**:
```csharp
public sealed class FlightInfo
{
    public string AirlineName { get; set; }        // "United Airlines"
    public string FlightNumber { get; set; }       // "UA123"
    public DateTime? FlightDateTime { get; set; }  // Scheduled arrival/departure (UTC)
}
```

**Usage**:
```csharp
var flightInfo = new FlightInfo
{
    AirlineName = "United Airlines",
    FlightNumber = "UA123",
    FlightDateTime = DateTime.Parse("2026-02-15T14:00:00Z")
};
```

---

### Enums

#### VehicleClass

**Location**: `BellwoodGlobal.Core/Domain/VehicleClass.cs`

**Values**:
```csharp
public enum VehicleClass
{
    Sedan,           // Standard sedan (4 passengers)
    SUV,             // SUV (6 passengers)
    ExecutiveSedan,  // Luxury sedan (4 passengers)
    Luxury SUV,      // Luxury SUV (6 passengers)
    Van,             // Passenger van (8+ passengers)
    Stretch,         // Stretch limo (10+ passengers)
    Sprinter,        // Mercedes Sprinter (14+ passengers)
    Bus              // Motor coach (30+ passengers)
}
```

**String Mapping** (for API):
```csharp
var vehicleClassString = quoteDraft.VehicleClass.ToString(); // "Sedan"
```

---

#### PickupStyle

**Location**: `BellwoodGlobal.Core/Domain/PickupStyle.cs`

**Values**:
```csharp
public enum PickupStyle
{
    Curbside,      // Meet at curb/outside
    MeetAndGreet   // Driver meets passenger inside with sign
}
```

**Usage**:
```csharp
quoteDraft.PickupStyle = PickupStyle.MeetAndGreet.ToString(); // "MeetAndGreet"
```

---

## ?? Response DTOs (AdminAPI ? Mobile)

### QuoteListItem

**Purpose**: Quote list row data (for QuoteDashboardPage)

**Schema**:
```csharp
public sealed class QuoteListItem
{
    public string Id { get; set; }                  // "quote-abc-123"
    public DateTime CreatedUtc { get; set; }        // When submitted
    public string Status { get; set; }              // "Pending", "Acknowledged", "Responded", "Accepted", "Cancelled"
    public string BookerName { get; set; }          // "John Doe"
    public string PassengerName { get; set; }       // "Jane Smith"
    public string VehicleClass { get; set; }        // "Sedan"
    public string PickupLocation { get; set; }      // "O'Hare Airport"
    public string DropoffLocation { get; set; }     // "Downtown Chicago"
    public DateTime PickupDateTime { get; set; }    // Requested pickup time (UTC)
    
    // Phase Alpha fields
    public decimal? EstimatedPrice { get; set; }    // Dispatcher's price (if responded)
    public DateTime? RespondedAt { get; set; }      // When dispatcher responded
}
```

**Usage**:
```csharp
// GET /quotes/list
var quotes = await _adminApi.GetQuotesAsync();

foreach (var quote in quotes)
{
    Console.WriteLine($"{quote.PassengerName} - {quote.Status} - ${quote.EstimatedPrice}");
}
```

---

### QuoteDetail

**Purpose**: Full quote details (for QuoteDetailPage)

**Schema**:
```csharp
public sealed class QuoteDetail
{
    // Basic Info
    public string Id { get; set; }
    public string Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string BookerName { get; set; }
    public string PassengerName { get; set; }
    public string VehicleClass { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    
    // Full Draft Data
    public QuoteDraft Draft { get; set; }
    
    // Lifecycle Metadata
    public string CreatedByUserId { get; set; }
    public string ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
    
    // Dispatcher Response
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string RespondedByUserId { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public DateTime? EstimatedPickupTime { get; set; }
    public string Notes { get; set; }
}
```

---

### AcceptQuoteResponse

**Purpose**: Result of accepting a quote

**Schema**:
```csharp
public sealed class AcceptQuoteResponse
{
    public string Message { get; set; }           // "Quote accepted and booking created successfully"
    public string QuoteId { get; set; }           // "quote-abc-123"
    public string QuoteStatus { get; set; }       // "Accepted"
    public string BookingId { get; set; }         // "booking-xyz-789"
    public string BookingStatus { get; set; }     // "Requested"
    public string SourceQuoteId { get; set; }     // "quote-abc-123"
}
```

---

### BookingListItem

**Purpose**: Booking list row data (for BookingsPage)

**Schema**:
```csharp
public sealed class BookingListItem
{
    public string Id { get; set; }                // "booking-xyz-789"
    public string RideId { get; set; }            // "ride-123" (if assigned)
    public string Status { get; set; }            // "Requested", "Confirmed", "InProgress", "Completed", "Cancelled"
    public string PassengerName { get; set; }
    public string BookerName { get; set; }
    public string VehicleClass { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTime CreatedUtc { get; set; }
    public decimal? ConfirmedPrice { get; set; }  // Final price (if confirmed)
}
```

---

### BookingDetail

**Purpose**: Full booking details (for BookingDetailPage)

**Schema**:
```csharp
public sealed class BookingDetail
{
    public string Id { get; set; }
    public string RideId { get; set; }
    public string Status { get; set; }
    public string CurrentRideStatus { get; set; }  // "OnRoute", "Arrived", "InProgress", "Completed"
    public DateTime CreatedUtc { get; set; }
    public string PassengerName { get; set; }
    public string BookerName { get; set; }
    public string VehicleClass { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public decimal? ConfirmedPrice { get; set; }
    
    // Full draft data
    public QuoteDraft Draft { get; set; }
    
    // Source quote (if created from quote)
    public string SourceQuoteId { get; set; }
}
```

---

### DriverLocation

**Purpose**: Real-time driver GPS location

**Schema**:
```csharp
public sealed class DriverLocation
{
    public double Latitude { get; set; }          // GPS latitude
    public double Longitude { get; set; }         // GPS longitude
    public DateTime Timestamp { get; set; }       // When location was captured (UTC)
    public double? Heading { get; set; }          // Direction in degrees (0-360)
    public double? SpeedKmh { get; set; }         // Speed in km/h
    public double AgeSeconds { get; set; }        // Age of location data
    public string DriverName { get; set; }        // Driver's name
}
```

**Usage**:
```csharp
// GET /passenger/rides/{rideId}/location
var location = await _adminApi.GetDriverLocationAsync(rideId);

Console.WriteLine($"Driver at ({location.Latitude}, {location.Longitude})");
Console.WriteLine($"Speed: {location.SpeedKmh} km/h");
Console.WriteLine($"Last updated: {location.AgeSeconds} seconds ago");
```

---

## ?? Client Models (Mobile-Specific)

### EtaResult

**Purpose**: Calculated ETA for driver tracking

**Schema**:
```csharp
public sealed class EtaResult
{
    public int EstimatedMinutes { get; set; }     // ETA in minutes
    public double DistanceKm { get; set; }        // Straight-line distance
    public bool IsEstimate { get; set; }          // True if using default speed
    
    public string DisplayText =>
        $"{EstimatedMinutes} min away ({DistanceKm:F2} km)";
}
```

**Usage**:
```csharp
var eta = _trackingService.CalculateEta(driverLocation, pickupLat, pickupLng);

EtaLabel.Text = eta.DisplayText; // "8 min away (3.21 km)"
```

---

### AutocompletePrediction

**Purpose**: Google Places autocomplete suggestion

**Schema**:
```csharp
public sealed class AutocompletePrediction
{
    public string PlaceId { get; set; }           // Google place ID
    public string Description { get; set; }       // Full address
    public string MainText { get; set; }          // Primary text (street address)
    public string SecondaryText { get; set; }     // Secondary text (city, state)
}
```

**Usage**:
```csharp
var predictions = await _placesService.GetPredictionsAsync("123 Main St");

foreach (var prediction in predictions)
{
    Console.WriteLine($"{prediction.MainText}, {prediction.SecondaryText}");
}
```

---

### PlaceDetails

**Purpose**: Detailed location info from Google Places

**Schema**:
```csharp
public sealed class PlaceDetails
{
    public string PlaceId { get; set; }
    public string FormattedAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
```

**Usage**:
```csharp
var details = await _placesService.GetPlaceDetailsAsync(placeId);

quoteDraft.PickupLocation = details.FormattedAddress;
quoteDraft.PickupLatitude = details.Latitude;
quoteDraft.PickupLongitude = details.Longitude;
```

---

### Location (Saved)

**Purpose**: User-saved favorite locations

**Schema**:
```csharp
public sealed class Location
{
    public string Id { get; set; }                // Unique identifier
    public string Label { get; set; }             // "Home", "Work", "Airport"
    public string Address { get; set; }           // Full address
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedUtc { get; set; }
}
```

---

## ??? Data Relationships

### Quote ? Booking Flow

```
QuoteDraft (submitted)
    ?
QuoteDetail (status: Pending)
    ?
QuoteDetail (status: Acknowledged)
    ?
QuoteDetail (status: Responded, with EstimatedPrice)
    ?
Accept Quote ? AcceptQuoteResponse
    ?
BookingDetail (created from quote, SourceQuoteId set)
    ?
BookingDetail (status: Confirmed, RideId assigned)
```

---

### Booking ? Ride ? Location Flow

```
BookingDetail
    ? RideId assigned
RideDetail (status: OnRoute)
    ? Driver starts tracking
DriverLocation (updated every 15s)
```

---

## ?? Model Validation

### QuoteDraft Validation Rules

```csharp
public class QuoteDraftValidator
{
    public ValidationResult Validate(QuoteDraft draft)
    {
        var errors = new List<string>();
        
        // Booker validation
        if (string.IsNullOrWhiteSpace(draft.Booker?.FirstName))
            errors.Add("Booker first name is required");
        if (string.IsNullOrWhiteSpace(draft.Booker?.EmailAddress))
            errors.Add("Booker email is required");
        if (!IsValidEmail(draft.Booker?.EmailAddress))
            errors.Add("Booker email is invalid");
        
        // Passenger validation
        if (string.IsNullOrWhiteSpace(draft.Passenger?.FirstName))
            errors.Add("Passenger first name is required");
        
        // Trip validation
        if (string.IsNullOrWhiteSpace(draft.VehicleClass))
            errors.Add("Vehicle class is required");
        if (draft.PickupDateTime < DateTime.UtcNow)
            errors.Add("Pickup time must be in the future");
        if (string.IsNullOrWhiteSpace(draft.PickupLocation))
            errors.Add("Pickup location is required");
        if (string.IsNullOrWhiteSpace(draft.DropoffLocation))
            errors.Add("Dropoff location is required");
        
        // Passenger count validation
        if (draft.PassengerCount < 1)
            errors.Add("Passenger count must be at least 1");
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** - AutocompletePrediction, PlaceDetails
- **[11-Location-Tracking.md](11-Location-Tracking.md)** - DriverLocation, EtaResult
- **[12-Quote-Lifecycle.md](12-Quote-Lifecycle.md)** - QuoteListItem, QuoteDetail, AcceptQuoteResponse
- **[20-API-Integration.md](20-API-Integration.md)** - API request/response formats
- **[23-Security-Model.md](23-Security-Model.md)** - Data isolation & ownership

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
