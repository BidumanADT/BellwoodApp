# Passenger Location Tracking - Quick Reference

## ?? Quick Start

### For Developers

**New Endpoint:**
```csharp
GET /passenger/rides/{rideId}/location
Authorization: Bearer {jwt_token}
```

**Key Changes:**
- ? Uses passenger-safe endpoint (no more 403!)
- ? Handles "tracking not started" gracefully
- ? New states: `NotStarted`, `Unauthorized`
- ? Clear error messages for users

---

## ?? Code Examples

### Check if Tracking is Active

```csharp
var response = await _http.GetAsync($"/passenger/rides/{rideId}/location");
var data = await response.Content.ReadFromJsonAsync<PassengerLocationResponse>();

if (!data.TrackingActive)
{
    // Driver hasn't started yet
    ShowMessage(data.Message); // "Driver has not started tracking yet"
    return;
}

// Tracking is active, show location
var location = data.ToDriverLocation();
UpdateMap(location.Latitude, location.Longitude);
```

---

### Handle Different States

```csharp
switch (_trackingService.CurrentState)
{
    case TrackingState.Loading:
        // Show spinner
        break;
        
    case TrackingState.Tracking:
        // Show live map with driver marker
        break;
        
    case TrackingState.NotStarted:
        // "Driver hasn't started the trip yet"
        break;
        
    case TrackingState.Unauthorized:
        // "You can only view your own bookings"
        HideRetryButton();
        break;
        
    case TrackingState.Unavailable:
        // "GPS signal unavailable"
        break;
        
    case TrackingState.Error:
        // "Connection error"
        ShowRetryButton();
        break;
}
```

---

### Calculate ETA

```csharp
var eta = _trackingService.CalculateEta(
    driverLocation, 
    pickupLatitude, 
    pickupLongitude
);

EtaLabel.Text = eta.DisplayText; // "8 min away"
DistanceLabel.Text = $"{eta.DistanceKm:F1} km away";

if (eta.IsEstimate)
{
    DistanceLabel.Text += " (est.)";
}
```

---

## ?? State Transitions

```
Loading
  ?
  ??? Tracking (location received)
  ??? NotStarted (driver hasn't started)
  ??? Unauthorized (403 forbidden)
  ??? Error (network error)
  ??? Unavailable (404 not found)

NotStarted
  ?
  ??? Tracking (driver starts ride)

Tracking
  ?
  ??? Unavailable (GPS lost)
  ??? Ended (ride completed)

Unauthorized
  ?
  (terminal state, stop polling)
```

---

## ?? Debugging

### Console Logs

```csharp
// Look for these in debug output
[DriverTrackingService] Location received: 41.878100, -87.629800, Age=5s
[DriverTrackingService] State changed to: Tracking
[DriverTrackingService] ETA: 8 min away, Distance: 3.21 km
[DriverTrackingService] Tracking not started: Driver has not started tracking yet
[DriverTrackingService] Unauthorized to view ride: booking-123
```

### Check API Response

```json
// Success (tracking active)
{
  "rideId": "booking-123",
  "trackingActive": true,
  "latitude": 41.8781,
  "longitude": -87.6298
}

// Success (not started)
{
  "rideId": "booking-123",
  "trackingActive": false,
  "message": "Driver has not started tracking yet"
}

// Error (unauthorized)
HTTP 403 Forbidden
{
  "error": "You can only view location for your own bookings"
}
```

---

## ??? Common Issues

### Issue: Still getting 403

**Solution:**
1. Verify backend has passenger endpoint
2. Check JWT token has email claim
3. Verify email matches booking

```csharp
// Debug: Log the email claim
var email = User.FindFirst("email")?.Value;
Console.WriteLine($"User email: {email}");
Console.WriteLine($"Booker email: {booking.Booker.Email}");
```

---

### Issue: "Not Started" forever

**Solution:**
1. Check driver app is sending location updates
2. Verify ride status is OnRoute or later
3. Verify rideId matches booking ID

```csharp
// Debug: Check ride status
Console.WriteLine($"Ride status: {booking.CurrentRideStatus ?? booking.Status}");
```

---

### Issue: Map not showing

**Solution:**
1. Verify lat/lng are valid
2. Check map permissions
3. Verify pickup coordinates exist

```csharp
// Debug: Log coordinates
Console.WriteLine($"Pickup: {_pickupLatitude}, {_pickupLongitude}");
Console.WriteLine($"Driver: {location.Latitude}, {location.Longitude}");
```

---

## ?? Testing Checklist

**Quick Test:**
```
1. ? Create booking as Alice (alice@example.com)
2. ? Login as Alice in passenger app
3. ? Driver starts ride ? status = OnRoute
4. ? Tap "Track Driver" in booking detail
5. ? Verify map shows driver location
6. ? Verify ETA displays
7. ? Verify status chip shows "Live" (gold)
8. ? Wait 15 seconds, verify location updates
```

**Edge Cases:**
```
1. ? Driver NOT started ? shows "waiting" message
2. ? Login as Bob ? try Alice's ride ? 403 "unauthorized"
3. ? Network off ? shows "connection error"
4. ? Driver completes ride ? shows "ended"
```

---

## ?? Documentation

**For Implementation Details:**
? `PassengerLocationTracking-Implementation.md`

**For Full Test Suite:**
? `PassengerLocationTracking-TestingGuide.md`

**For Executive Summary:**
? `PassengerLocationTracking-Summary.md`

---

## ?? Key Files

```
Models/
  ?? DriverLocation.cs          # PassengerLocationResponse model

Services/
  ?? DriverTrackingService.cs   # Uses passenger endpoint

Pages/
  ?? DriverTrackingPage.xaml    # UI with status chip
  ?? DriverTrackingPage.xaml.cs # State handling
```

---

## ? Performance Tips

**Reduce Battery Usage:**
```csharp
// Increase polling interval for low battery
int interval = Battery.State == BatteryState.Low 
    ? 30000  // 30 seconds
    : 15000; // 15 seconds

await _trackingService.StartTrackingAsync(rideId, lat, lng, interval);
```

**Reduce Data Usage:**
```csharp
// Stop polling when app is backgrounded
protected override void OnDisappearing()
{
    base.OnDisappearing();
    _trackingService.StopTracking();
}
```

---

## ?? Security Notes

**What's Protected:**
- ? Email-based authorization
- ? JWT token validation
- ? Passenger can only view own bookings

**What's NOT Protected (yet):**
- ?? No rate limiting (planned)
- ?? No PassengerId verification (planned)
- ?? SignalR subscriptions not authorized (planned)

---

## ?? What's New in v2.0

| Feature | Status |
|---------|--------|
| Passenger-safe endpoint | ? Done |
| "Not started" messaging | ? Done |
| Unauthorized handling | ? Done |
| State-specific messages | ? Done |
| Driver name display | ? Done |
| SignalR real-time | ?? v2.1 |
| Push notifications | ?? v2.3 |

---

## ?? Support

**Questions?**
- Check implementation guide first
- Search console logs for `[DriverTrackingService]`
- Verify backend is v1.3.0+

**Found a bug?**
- Check known issues in summary doc
- Verify test case passes in testing guide
- Log issue with reproduction steps

---

**Version:** 2.0.0  
**Last Updated:** December 2024  
**Build Status:** ? Passing  
**Test Coverage:** 25 cases documented
