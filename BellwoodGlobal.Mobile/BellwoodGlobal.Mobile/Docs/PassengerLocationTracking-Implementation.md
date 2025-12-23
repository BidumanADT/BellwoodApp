# Passenger Real-Time Location Tracking Implementation

## Overview

This document details the implementation of real-time driver location tracking for passengers in the BellwoodGlobal Mobile App. The solution uses a secure, passenger-safe endpoint and handles various tracking states gracefully.

---

## ?? Problem Solved

### Previous Issues
- ? Passengers getting `403 Forbidden` when accessing `/driver/location/{rideId}`
- ? No graceful handling of "tracking not started" state
- ? Confusing error messages for passengers
- ? Polling admin-only endpoints without proper authorization

### Solution Implemented
- ? New passenger-safe endpoint `/passenger/rides/{rideId}/location`
- ? Email-based authorization (passenger must own the booking)
- ? Graceful handling of "not started" state
- ? Clear, user-friendly error messages
- ? Proper authorization with JWT tokens via `AuthHttpHandler`

---

## ??? Architecture Changes

### API Endpoint Migration

**Before:**
```csharp
// Used admin/driver endpoint (403 Forbidden for passengers)
GET /driver/location/{rideId}
```

**After:**
```csharp
// Uses passenger-safe endpoint
GET /passenger/rides/{rideId}/location
```

### Response Handling

**Before:**
```csharp
// Only returned location or 404
DriverLocation | 404 Not Found
```

**After:**
```csharp
// Returns structured response with tracking status
PassengerLocationResponse {
    TrackingActive: bool,
    Message: string,
    Location: {...} or null
}
```

---

## ?? Files Modified

### 1. Models (`DriverLocation.cs`)

#### Added PassengerLocationResponse Model

```csharp
public sealed class PassengerLocationResponse
{
    public string RideId { get; set; }
    public bool TrackingActive { get; set; }
    public string? Message { get; set; }
    public string? CurrentStatus { get; set; }
    
    // Location fields (only present when TrackingActive = true)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double? AgeSeconds { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
    
    // Conversion helper
    public DriverLocation? ToDriverLocation() { ... }
}
```

#### Enhanced DriverLocation Model

```csharp
public sealed class DriverLocation
{
    // ...existing properties...
    
    // NEW: Driver identification
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
}
```

#### New Tracking States

```csharp
public enum TrackingState
{
    Loading,        // Initial loading
    Tracking,       // Actively tracking
    Unavailable,    // Temporarily unavailable
    NotStarted,     // NEW: Driver hasn't started yet
    Error,          // Connection error
    Ended,          // Ride completed
    Unauthorized    // NEW: Not authorized to view this ride
}
```

---

### 2. Service (`DriverTrackingService.cs`)

#### Updated GetDriverLocationAsync Method

**Key Changes:**
1. Uses `/passenger/rides/{rideId}/location` endpoint
2. Handles `403 Forbidden` ? `TrackingState.Unauthorized`
3. Parses `PassengerLocationResponse`
4. Sets `TrackingState.NotStarted` when `TrackingActive = false`

```csharp
public async Task<DriverLocation?> GetDriverLocationAsync(string rideId)
{
    try
    {
        // Use passenger-safe endpoint
        var response = await _http.GetAsync(
            $"/passenger/rides/{Uri.EscapeDataString(rideId)}/location");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Ride doesn't exist
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            // Not authorized to view this ride
            SetState(TrackingState.Unauthorized);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var passengerResponse = await response.Content
            .ReadFromJsonAsync<PassengerLocationResponse>(_jsonOptions);

        // Check if tracking has started
        if (!passengerResponse.TrackingActive)
        {
            SetState(TrackingState.NotStarted);
            return null;
        }

        // Convert to DriverLocation
        return passengerResponse.ToDriverLocation();
    }
    catch (HttpRequestException ex)
    {
        // Handle network errors
        return null;
    }
}
```

---

### 3. UI (`DriverTrackingPage.xaml` and `.xaml.cs`)

#### XAML Changes

Added named label for dynamic messaging:

```xml
<Label x:Name="UnavailableLabel"
       Text="Default message" 
       TextColor="{StaticResource BellwoodCream}" 
       HorizontalTextAlignment="Center"
       Opacity="0.8" />
```

#### Code-Behind Changes

Enhanced `UpdateStateUI` with state-specific messaging:

```csharp
private void UpdateStateUI(TrackingState state)
{
    switch (state)
    {
        case TrackingState.NotStarted:
            LoadingOverlay.IsVisible = false;
            UnavailableOverlay.IsVisible = true;
            UnavailableLabel.Text = 
                "Your driver hasn't started the trip yet.\n\n" +
                "Tracking will begin when your driver is en route.";
            StatusLabel.Text = "Waiting";
            StatusFrame.BackgroundColor = Colors.Orange;
            break;

        case TrackingState.Unauthorized:
            LoadingOverlay.IsVisible = false;
            UnavailableOverlay.IsVisible = true;
            UnavailableLabel.Text = 
                "You are not authorized to view this ride.\n\n" +
                "You can only track your own bookings.";
            StatusLabel.Text = "Error";
            StatusFrame.BackgroundColor = Colors.IndianRed;
            RetryButton.IsVisible = false; // Don't allow retry
            break;

        case TrackingState.Unavailable:
            UnavailableLabel.Text = 
                "Driver location temporarily unavailable.\n\n" +
                "This can happen due to poor GPS signal or network connectivity.";
            // ...
            break;

        case TrackingState.Error:
            UnavailableLabel.Text = 
                "Unable to connect to tracking service.\n\n" +
                "Please check your internet connection and try again.";
            RetryButton.IsVisible = true;
            break;
    }
}
```

---

## ?? Authorization Flow

### How It Works

```
PassengerApp
    ?
GET /passenger/rides/{rideId}/location
Headers:
  Authorization: Bearer {jwt_token}  ? Contains user email claim
    ?
AdminAPI
    ?? Extract email from JWT token
    ?? Load booking from storage
    ?? Check if email matches booker OR passenger
    ?? Return location if authorized
    ?? Return 403 if not authorized
    ?
PassengerApp
    ?? 200 OK ? Parse response
    ?? TrackingActive = true ? Show location
    ?? TrackingActive = false ? Show "not started" message
    ?? 403 Forbidden ? Show "unauthorized" message
```

### Email Matching Logic (Backend)

```csharp
bool isPassengerAuthorized = false;

// Check booker email
if (userEmail == booking.Draft.Booker.EmailAddress)
    isPassengerAuthorized = true;

// Check passenger email (if different from booker)
if (userEmail == booking.Draft.Passenger.EmailAddress)
    isPassengerAuthorized = true;

// Future: Check PassengerId claim
// if (userSub == booking.PassengerId) ...

if (!isPassengerAuthorized)
    return Forbid();
```

---

## ?? API Responses

### Response: Tracking Active

```json
{
  "rideId": "booking-123",
  "trackingActive": true,
  "latitude": 41.8781,
  "longitude": -87.6298,
  "timestamp": "2024-12-18T15:30:15Z",
  "heading": 45.5,
  "speed": 12.3,
  "accuracy": 8.5,
  "ageSeconds": 5.2,
  "driverUid": "driver-001",
  "driverName": "Charlie Johnson"
}
```

**App Behavior:**
- ? Shows driver marker on map
- ? Displays ETA and distance
- ? Status chip shows "Live" (gold)
- ? Polls every 15 seconds for updates

---

### Response: Tracking Not Started

```json
{
  "rideId": "booking-123",
  "trackingActive": false,
  "message": "Driver has not started tracking yet",
  "currentStatus": "Scheduled"
}
```

**App Behavior:**
- ? Hides loading overlay
- ? Shows unavailable overlay
- ? Message: "Your driver hasn't started the trip yet"
- ? Status chip shows "Waiting" (orange)
- ? Continues polling (driver might start soon)

---

### Response: Unauthorized (403)

```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "error": "You can only view location for your own bookings"
}
```

**App Behavior:**
- ? Shows unavailable overlay
- ? Message: "You are not authorized to view this ride"
- ? Status chip shows "Error" (red)
- ? Hides retry button (retry won't help)
- ? Stops polling

---

### Response: Not Found (404)

```http
HTTP/1.1 404 Not Found
```

**App Behavior:**
- ? Returns `null` from `GetDriverLocationAsync`
- ? Sets state to `Unavailable` (first fetch) or keeps last known
- ? Continues polling

---

## ?? UI States

### State Matrix

| Tracking State | Overlay Visible | Message | Status Chip | Retry Button |
|----------------|-----------------|---------|-------------|--------------|
| **Loading** | Loading | "Locating your driver..." | "Loading" (gray) | Hidden |
| **Tracking** | None | N/A | "Live" (gold) | Hidden |
| **NotStarted** | Unavailable | "Driver hasn't started yet" | "Waiting" (orange) | Visible |
| **Unavailable** | Unavailable | "GPS signal unavailable" | "Waiting" (orange) | Visible |
| **Unauthorized** | Unavailable | "Not authorized" | "Error" (red) | Hidden |
| **Error** | Unavailable | "Connection error" | "Error" (red) | Visible |
| **Ended** | None | "Ride ended" | "Ended" (gray) | Hidden |

---

## ?? Testing Scenarios

### Scenario 1: Normal Tracking Flow

**Steps:**
1. Passenger opens tracking page
2. Driver has started ride (status = OnRoute)
3. Driver is sending location updates

**Expected:**
- ? `TrackingState.Loading` initially
- ? `TrackingState.Tracking` after first location received
- ? Driver marker appears on map
- ? ETA and distance displayed
- ? Status chip shows "Live" (gold)
- ? Polls every 15 seconds

---

### Scenario 2: Tracking Not Started

**Steps:**
1. Passenger opens tracking page
2. Booking exists but driver hasn't started yet (status = Scheduled)

**Expected:**
- ? `TrackingState.Loading` initially
- ? `TrackingState.NotStarted` after first response
- ? Message: "Your driver hasn't started the trip yet"
- ? Status chip shows "Waiting" (orange)
- ? Continues polling every 15 seconds
- ? When driver starts ? transitions to `Tracking`

---

### Scenario 3: Unauthorized Access

**Steps:**
1. Passenger Alice tries to view Bob's ride
2. Email in JWT token doesn't match booking

**Expected:**
- ? `TrackingState.Loading` initially
- ? `403 Forbidden` response
- ? `TrackingState.Unauthorized`
- ? Message: "You are not authorized to view this ride"
- ? Status chip shows "Error" (red)
- ? Retry button hidden
- ? Polling stops

---

### Scenario 4: GPS Signal Lost

**Steps:**
1. Passenger is tracking driver
2. Driver loses GPS signal or network connection
3. Backend returns `TrackingActive = false`

**Expected:**
- ? Previously tracking
- ? `TrackingState.NotStarted` or `Unavailable`
- ? Last known marker remains on map
- ? Message: "GPS signal unavailable"
- ? Continues polling
- ? When signal returns ? transitions back to `Tracking`

---

### Scenario 5: Network Error

**Steps:**
1. Passenger loses internet connection
2. HTTP request fails

**Expected:**
- ? `TrackingState.Error` (if first fetch)
- ? Message: "Unable to connect to tracking service"
- ? Retry button visible
- ? Tapping retry restarts tracking

---

## ?? Configuration

### Polling Interval

Default: **15 seconds**

```csharp
await _trackingService.StartTrackingAsync(
    rideId, 
    pickupLatitude, 
    pickupLongitude, 
    pollingIntervalMs: 15000  // 15 seconds
);
```

**Considerations:**
- ? Shorter interval = more real-time, higher battery/data usage
- ?? Longer interval = better battery, less real-time
- ?? Backend rate limiting may apply

---

### Stale Location Threshold

Default: **120 seconds** (2 minutes)

```csharp
public bool IsStale => AgeSeconds > 120;
```

**UI Behavior:**
- Shows "? Location may be outdated" warning
- Pin address changes to "Last known location"

---

## ?? Future Enhancements

### 1. SignalR Real-Time Updates

Replace polling with SignalR subscriptions:

```csharp
// Connect to location hub
await _hubConnection.StartAsync();
await _hubConnection.InvokeAsync("SubscribeToRide", rideId);

// Handle location updates
_hubConnection.On<LocationUpdate>("LocationUpdate", (data) =>
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        UpdateDriverMarker(data.Latitude, data.Longitude);
    });
});
```

**Benefits:**
- ? Instant updates (no 15-second delay)
- ? Reduced battery usage (no polling)
- ? Lower server load

---

### 2. Push Notifications

Notify passengers when status changes:

```csharp
// When driver starts ride
"Your driver is on the way! ??"

// When driver arrives
"Your driver has arrived! ??"
```

---

### 3. ETA from Routing API

Use Google Maps Directions API for accurate ETAs:

```csharp
var directionsResponse = await _mapsApi.GetDirectionsAsync(
    driverLocation, 
    pickupLocation,
    mode: "driving",
    trafficModel: "best_guess"
);

var eta = directionsResponse.Routes[0].Duration;
```

---

### 4. Route Polyline

Show driver's route on map:

```csharp
var polyline = new Polyline
{
    StrokeColor = Colors.Blue,
    StrokeWidth = 5
};

foreach (var point in routePoints)
{
    polyline.Geopath.Add(new Location(point.Lat, point.Lng));
}

TrackingMap.MapElements.Add(polyline);
```

---

### 5. Driver Photo & Vehicle Info

Display driver details:

```csharp
DriverNameLabel.Text = location.DriverName;
DriverPhotoImage.Source = location.DriverPhotoUrl;
VehicleLabel.Text = $"{location.VehicleMake} {location.VehicleModel}";
LicensePlateLabel.Text = location.LicensePlate;
```

---

## ?? Performance Metrics

### Network Usage

| Scenario | Requests/Min | Data Usage |
|----------|--------------|------------|
| Polling (15s) | 4 | ~2 KB/min |
| SignalR (real-time) | ~0 | ~0.5 KB/min |

### Battery Impact

| Method | Battery Drain |
|--------|---------------|
| Polling | Moderate |
| SignalR | Low |
| Background | High (not implemented) |

---

## ?? Troubleshooting

### Issue: Still Getting 403 Forbidden

**Possible Causes:**
1. Backend not deployed with passenger endpoint
2. JWT token expired
3. Email claim not in token

**Solutions:**
1. Verify backend has `/passenger/rides/{id}/location` endpoint
2. Check token expiration in `AuthService`
3. Inspect JWT claims in debugger

---

### Issue: "Not Started" Forever

**Possible Causes:**
1. Driver app not sending location updates
2. Driver status not set to OnRoute
3. Ride ID mismatch

**Solutions:**
1. Verify driver app is updating location
2. Check ride status in admin portal
3. Verify rideId matches booking ID

---

### Issue: Map Not Centering Properly

**Possible Causes:**
1. Invalid coordinates
2. Pickup coordinates (0, 0)
3. Distance calculation error

**Solutions:**
1. Verify lat/lng are valid ranges
2. Check booking has pickup coordinates
3. Add logging to `UpdateMapView`

---

## ?? Related Documentation

- `CurrentRideStatus-PassengerApp-Fix.md` - Status display fix
- `Bookings-Access-And-Tracking-Fix.md` - Booking access
- `DriverTracking-DateTimeFix-Implementation.md` - DateTime handling
- Backend: `PASSENGER_LOCATION_ENDPOINT_SUMMARY.md`

---

## ? Verification Checklist

**Code Changes:**
- [x] `PassengerLocationResponse` model added
- [x] `DriverLocation` updated with driver info
- [x] `TrackingState` enum extended
- [x] `GetDriverLocationAsync` uses passenger endpoint
- [x] 403 handling ? `Unauthorized` state
- [x] `NotStarted` state handling
- [x] UI messages updated
- [x] XAML label named for dynamic updates

**Testing:**
- [x] Build successful
- [ ] Normal tracking works
- [ ] "Not started" message displays
- [ ] Unauthorized handled gracefully
- [ ] Network errors handled
- [ ] Retry button works

**Documentation:**
- [x] Implementation guide created
- [x] API responses documented
- [x] UI states documented
- [x] Future enhancements listed

---

**Date**: December 2024  
**Version**: 2.0.0  
**Status**: ? IMPLEMENTED  
**Breaking Changes**: None (backward compatible)  
**Next Steps**: Deploy backend, test with live data, consider SignalR
