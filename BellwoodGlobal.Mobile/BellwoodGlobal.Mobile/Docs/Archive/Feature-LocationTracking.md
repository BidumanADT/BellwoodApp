# Location Tracking - Complete Reference

**Feature:** Real-time driver location tracking for passengers  
**Status:** ? Complete  
**Last Updated:** January 10, 2026

---

## ?? Quick Summary

**What it does:** Allows passengers to track their driver's real-time location during a ride.

**Key Components:**
- ?? Real-time GPS tracking
- ?? ETA calculation (distance + time)
- ??? Map integration
- ?? Authorization (email-based)

**Files Involved:**
- `Services/DriverTrackingService.cs` - Polling and state management
- `Services/AdminApi.cs` - Backend API integration
- `Pages/DriverTrackingPage.xaml` - UI with map
- `Models/DriverLocation.cs` - Location data model

---

## ??? Architecture

### Backend Integration

```
Mobile App
    ?
GET /passenger/rides/{rideId}/location
Authorization: Bearer {jwt_token}
    ?
AdminAPI validates: user.email == booking.booker.email
    ?
Returns: PassengerLocationResponse
{
  "trackingActive": true/false,
  "latitude": 41.8781,
  "longitude": -87.6298,
  "timestamp": "2024-12-18T15:30:15Z",
  "heading": 45.5,
  "speed": 12.3,
  "ageSeconds": 5.2
}
```

### Service Layer

```
DriverTrackingService
??? StartTrackingAsync(rideId, pickupLat, pickupLng)
??? StopTracking()
??? GetDriverLocationAsync(rideId) ? DriverLocation?
??? CalculateEta(location, pickupLat, pickupLng) ? EtaResult

Events:
??? LocationUpdated (DriverLocation)
??? StateChanged (TrackingState)
??? EtaUpdated (EtaResult)

TrackingState Enum:
- Loading
- Tracking
- NotStarted
- Unavailable
- Unauthorized
- Error
```

### Polling Loop

```
StartTrackingAsync()
    ?
PollLocationAsync() runs every 15 seconds
    ?
GetDriverLocationAsync(rideId)
    ?
If location received:
    - Emit LocationUpdated event
    - Calculate ETA
    - Emit EtaUpdated event
    - State = Tracking
    ?
If not started:
    - State = NotStarted
    ?
If unauthorized:
    - State = Unauthorized
    - Stop polling
```

---

## ?? Authorization

### Email-Based Access Control

**Backend validates:**
```csharp
var userEmail = User.FindFirst("email")?.Value;
var booking = await GetBookingAsync(rideId);

if (userEmail != booking.Booker.EmailAddress && 
    userEmail != booking.Passenger.EmailAddress)
{
    return Forbid(); // 403 Forbidden
}
```

**Mobile app handles:**
- 403 Forbidden ? State = Unauthorized
- 404 Not Found ? State = Unavailable
- 200 OK, trackingActive=false ? State = NotStarted

---

## ?? ETA Calculation

### Haversine Formula

```csharp
public EtaResult CalculateEta(
    DriverLocation driverLocation, 
    double pickupLat, 
    double pickupLng)
{
    // Calculate distance using Haversine formula
    var distanceKm = CalculateDistanceKm(
        driverLocation.Latitude, 
        driverLocation.Longitude,
        pickupLat, 
        pickupLng);
    
    // Use driver's actual speed if available
    var speedKmh = driverLocation.SpeedKmh > 0 
        ? driverLocation.SpeedKmh.Value 
        : AverageSpeedKmh; // 35 km/h default
    
    // Calculate ETA in minutes
    var etaMinutes = (int)Math.Ceiling((distanceKm / speedKmh) * 60);
    
    return new EtaResult
    {
        EstimatedMinutes = etaMinutes,
        DistanceKm = distanceKm,
        IsEstimate = driverLocation.SpeedKmh == null
    };
}
```

---

## ?? Known Issues & Fixes

### Issue 1: DateTime Double Conversion

**Problem:** UTC timestamps were being converted to local time twice, causing incorrect dates.

**Root Cause:**
```csharp
// BEFORE (wrong):
var timestamp = DateTime.UtcNow; // Already UTC
var local = timestamp.ToLocalTime(); // First conversion
var display = local.ToString("g"); // UI shows local
// Problem: Backend already sent UTC, app converted again

// AFTER (fixed):
var timestamp = response.Timestamp; // Already UTC
var display = timestamp.ToLocalTime().ToString("g"); // Single conversion
```

**Fix:** Created `DateTimeHelper.FormatFriendly()` to handle conversions consistently.

**Status:** ? Fixed (Dec 18, 2025)

---

### Issue 2: Polling Loop State Bug

**Problem:** State stayed "Loading" even when location received.

**Root Cause:**
```csharp
// BEFORE:
if (location != null)
{
    LastKnownLocation = location;
    LocationUpdated?.Invoke(this, location);
    // Missing: SetState(TrackingState.Tracking);
}

// AFTER:
if (location != null)
{
    LastKnownLocation = location;
    SetState(TrackingState.Tracking); // Now sets state correctly
    LocationUpdated?.Invoke(this, location);
}
```

**Status:** ? Fixed (Dec 18, 2025)

---

### Issue 3: CurrentRideStatus Not Displayed

**Problem:** Booking detail page showed generic status instead of driver-specific status.

**Root Cause:**
```csharp
// BEFORE:
var displayStatus = ToDisplayStatus(booking.Status);
// Always used booking.Status, ignored CurrentRideStatus

// AFTER:
var effectiveStatus = !string.IsNullOrWhiteSpace(booking.CurrentRideStatus) 
    ? booking.CurrentRideStatus 
    : booking.Status;
var displayStatus = ToDisplayStatus(effectiveStatus);
```

**Status:** ? Fixed (Dec 18, 2025)

---

## ?? Testing

### Test Scenario 1: Happy Path ?

**Setup:**
1. Create booking as Alice
2. Driver starts ride (status ? OnRoute)
3. Driver sends location updates

**Steps:**
1. Login as Alice
2. Navigate to booking detail
3. Tap "Track Driver"

**Expected:**
- ? Map loads with pickup pin
- ? Driver marker appears within 15 seconds
- ? ETA and distance displayed
- ? Status chip shows "Driver En Route" (gold)

---

### Test Scenario 2: Tracking Not Started ?

**Setup:**
1. Create booking
2. Driver assigned but hasn't started

**Steps:**
1. Tap "Track Driver"

**Expected:**
- ? Loading overlay appears
- ? "Driver hasn't started trip yet" message
- ? Status chip shows "Waiting" (orange)
- ? Continues polling every 15 seconds

---

### Test Scenario 3: Unauthorized Access ?

**Setup:**
1. Booking A belongs to Alice
2. Booking B belongs to Bob

**Steps:**
1. Login as Bob
2. Try to view Alice's booking tracking

**Expected:**
- ? Unavailable overlay shows
- ? Message: "Not authorized to view this ride"
- ? Status chip shows "Error" (red)
- ? Polling stops

---

## ?? API Reference

### DriverTrackingService

```csharp
public interface IDriverTrackingService
{
    // Start tracking
    Task StartTrackingAsync(
        string rideId, 
        double pickupLatitude, 
        double pickupLongitude, 
        int pollingIntervalMs = 15000);
    
    // Stop tracking
    void StopTracking();
    
    // Get current location
    Task<DriverLocation?> GetDriverLocationAsync(string rideId);
    
    // Calculate ETA
    EtaResult CalculateEta(
        DriverLocation driverLocation, 
        double pickupLatitude, 
        double pickupLongitude);
    
    // Events
    event EventHandler<DriverLocation>? LocationUpdated;
    event EventHandler<TrackingState>? StateChanged;
    event EventHandler<EtaResult>? EtaUpdated;
    
    // Properties
    TrackingState CurrentState { get; }
    DriverLocation? LastKnownLocation { get; }
    EtaResult? LastKnownEta { get; }
}
```

### Models

```csharp
public class DriverLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? SpeedKmh { get; set; }
    public double AgeSeconds { get; set; }
    public string? DriverName { get; set; }
}

public class EtaResult
{
    public int EstimatedMinutes { get; set; }
    public double DistanceKm { get; set; }
    public bool IsEstimate { get; set; }
    
    public string DisplayText => 
        $"{EstimatedMinutes} min away ({DistanceKm:F2} km)";
}

public enum TrackingState
{
    Loading,
    Tracking,
    NotStarted,
    Unavailable,
    Unauthorized,
    Error
}
```

---

## ?? Debugging

### Enhanced Logging

```csharp
#if DEBUG
System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] >>> Fetching location for ride: {rideId}");
System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] >>> HTTP Status: {(int)response.StatusCode}");
System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] >>> TrackingActive: {passengerResponse.TrackingActive}");
System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] Location received: {location.Latitude:F6}, {location.Longitude:F6}");
#endif
```

### Common Log Patterns

**Success:**
```
[DriverTrackingService] START TRACKING CALLED
[DriverTrackingService] RideId: abc123
[DriverTrackingService] >>> HTTP Status: 200 OK
[DriverTrackingService] >>> TrackingActive: TRUE
[DriverTrackingService] Location received: 41.878100, -87.629800
[DriverTrackingService] ETA: 8 min away, Distance: 3.21 km
```

**Not Started:**
```
[DriverTrackingService] >>> TrackingActive: FALSE
[DriverTrackingService] >>> Message: Driver has not started tracking yet
[DriverTrackingService] State changed to: NotStarted
```

**Unauthorized:**
```
[DriverTrackingService] !!!FORBIDDEN!!! Unauthorized to view ride: abc123
[DriverTrackingService] State changed to: Unauthorized
```

---

## ?? Implementation Timeline

- **Initial Implementation:** Dec 15, 2025
- **DateTime Fix:** Dec 18, 2025
- **Polling Loop Fix:** Dec 18, 2025
- **CurrentRideStatus Fix:** Dec 18, 2025
- **Enhanced Logging:** Dec 18, 2025
- **Documentation:** Dec 18, 2025

**Total Effort:** ~12 hours

---

## ?? Related Documentation

- `Feature-GooglePlacesAutocomplete.md` - Address autocomplete (pickup coordinates)
- `Reference-BugFixes.md` - All bug fix history
- `Guide-ConfigurationSecurity.md` - API configuration

---

## ?? Future Enhancements

### Potential Improvements
- Real-time updates via WebSockets (replace polling)
- Route preview (show driver's path)
- Traffic-aware ETA (use routing API)
- Notification when driver arrives
- Background tracking (continue when app backgrounded)

### Nice-to-Have Features
- Driver photo and vehicle info
- Chat with driver
- Share ETA with others
- Trip history with route replay

---

**Status:** ? **COMPLETE - PRODUCTION READY**  
**Version:** 1.0  
**Maintainer:** Development Team
