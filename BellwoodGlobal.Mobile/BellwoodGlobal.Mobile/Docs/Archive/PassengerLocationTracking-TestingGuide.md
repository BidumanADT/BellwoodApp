# Passenger Location Tracking - Testing Guide

## Pre-Test Setup

### Backend Requirements

? **Verify Backend Has Passenger Endpoint:**
```bash
curl -X GET "https://api.bellwood.com/passenger/rides/{rideId}/location" \
  -H "Authorization: Bearer {passenger_token}"
```

Expected: `200 OK` or `403 Forbidden` (not 404)

? **Verify Authorization Works:**
- Passenger who owns booking ? `200 OK`
- Passenger who doesn't own booking ? `403 Forbidden`
- No token ? `401 Unauthorized`

? **Verify Tracking States:**
- Driver hasn't started ? `TrackingActive: false`
- Driver started ? `TrackingActive: true` with coordinates

---

### Test Environment

**Devices Needed:**
- Driver app (iOS/Android emulator or physical device)
- Passenger app (iOS/Android emulator or physical device)
- Both connected to same backend instance

**Test Data:**
- At least 2 confirmed bookings:
  - Booking A: Passenger Alice (alice@example.com)
  - Booking B: Passenger Bob (bob@example.com)
- Both scheduled for testing timeframe

---

## Test Cases

### ? TC001: Normal Tracking Flow

**Preconditions:**
- Booking confirmed for Alice
- Driver assigned to ride
- Driver app installed and logged in

**Steps:**
1. Driver app: Select Alice's ride
2. Driver app: Tap "Start Ride" ? Status changes to OnRoute
3. Driver app: GPS starts sending location updates
4. Passenger app (Alice): Navigate to booking detail
5. Passenger app: Tap "Track Driver" button

**Expected Results:**
- ? Map page opens with pickup pin
- ? Loading overlay shows "Locating your driver..."
- ? Within 15 seconds, driver marker appears on map
- ? Status chip shows "Live" (gold color)
- ? ETA displays (e.g., "8 min away")
- ? Distance displays (e.g., "3.2 km away")
- ? Map auto-zooms to show both pins
- ? Driver marker updates every ~15 seconds
- ? Last updated label shows "Just now" or "X seconds ago"

**Console Logs:**
```
[DriverTrackingService] Location received: 41.878100, -87.629800, Age=5s
[DriverTrackingService] State changed to: Tracking
[DriverTrackingService] ETA: 8 min away, Distance: 3.21 km
```

**API Request:**
```http
GET /passenger/rides/booking-123/location
Authorization: Bearer eyJhbGci...
```

**API Response:**
```json
{
  "rideId": "booking-123",
  "trackingActive": true,
  "latitude": 41.8781,
  "longitude": -87.6298,
  "timestamp": "2024-12-18T15:30:15Z",
  "ageSeconds": 5
}
```

---

### ? TC002: Tracking Not Started

**Preconditions:**
- Booking confirmed for Alice
- Driver assigned but hasn't started yet
- Ride status = "Scheduled"

**Steps:**
1. Passenger app (Alice): Navigate to booking detail
2. Passenger app: Tap "Track Driver" button

**Expected Results:**
- ? Map page opens with pickup pin
- ? Loading overlay shows briefly
- ? Unavailable overlay appears
- ? Message: "Your driver hasn't started the trip yet. Tracking will begin when your driver is en route."
- ? Status chip shows "Waiting" (orange)
- ? Retry button is visible
- ? App continues polling every 15 seconds

**Then:**
1. Driver app: Tap "Start Ride"
2. Wait up to 15 seconds

**Expected After Driver Starts:**
- ? Unavailable overlay disappears
- ? Driver marker appears on map
- ? Status chip changes to "Live" (gold)
- ? ETA displays

**Console Logs:**
```
[DriverTrackingService] Tracking not started: Driver has not started tracking yet
[DriverTrackingService] State changed to: NotStarted
... (15 seconds later) ...
[DriverTrackingService] Location received: 41.878100, -87.629800
[DriverTrackingService] State changed to: Tracking
```

**API Response (before driver starts):**
```json
{
  "rideId": "booking-123",
  "trackingActive": false,
  "message": "Driver has not started tracking yet",
  "currentStatus": "Scheduled"
}
```

---

### ? TC003: Unauthorized Access

**Preconditions:**
- Booking A belongs to Alice (alice@example.com)
- Booking B belongs to Bob (bob@example.com)
- Bob is logged into passenger app

**Steps:**
1. Passenger app (Bob): Manually navigate to Alice's booking tracking page
   - Use deep link or query parameter: `?rideId=alice-booking-123`
2. Page attempts to load

**Expected Results:**
- ? Loading overlay shows briefly
- ? Unavailable overlay appears
- ? Message: "You are not authorized to view this ride. You can only track your own bookings."
- ? Status chip shows "Error" (red)
- ? Retry button is HIDDEN (retry won't help)
- ? Polling STOPS (no repeated 403 errors)

**Console Logs:**
```
[DriverTrackingService] Unauthorized to view ride: alice-booking-123
[DriverTrackingService] State changed to: Unauthorized
```

**API Response:**
```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "error": "You can only view location for your own bookings"
}
```

---

### ? TC004: Ride Not Found

**Preconditions:**
- Invalid or non-existent ride ID

**Steps:**
1. Passenger app: Navigate to tracking page with invalid ID
   - `?rideId=invalid-123`

**Expected Results:**
- ? Loading overlay shows
- ? 404 response from API
- ? Unavailable overlay appears
- ? Message: "Driver location temporarily unavailable..." (generic)
- ? Status chip shows "Waiting" (orange)
- ? Retry button visible

**API Response:**
```http
HTTP/1.1 404 Not Found
```

---

### ? TC005: GPS Signal Lost

**Preconditions:**
- Driver is actively tracking (status = OnRoute)
- Passenger is viewing live tracking

**Steps:**
1. Passenger app: Driver marker visible on map
2. Driver app: Simulate GPS loss
   - Turn off location services
   - OR move to area with no GPS signal
3. Wait 2-3 minutes

**Expected Results:**
- ? Driver marker remains at last known position
- ? Last updated label shows "2 minutes ago"
- ? Stale warning appears: "? Location may be outdated"
- ? Status chip remains "Live" (gold) OR changes to "Waiting" (orange)
- ? App continues polling

**Then:**
1. Driver app: Re-enable GPS
2. Wait up to 15 seconds

**Expected After GPS Returns:**
- ? Driver marker updates to new position
- ? Last updated label shows "Just now"
- ? Stale warning disappears
- ? Status chip shows "Live" (gold)

---

### ? TC006: Network Connection Lost (Passenger)

**Preconditions:**
- Passenger is viewing live tracking
- Driver is actively sending location updates

**Steps:**
1. Passenger app: Tracking active
2. Passenger device: Turn off Wi-Fi and cellular data
3. Wait 15-30 seconds

**Expected Results:**
- ? HTTP request fails
- ? Driver marker remains at last known position
- ? Status chip changes to "Waiting" or "Error"
- ? (On first failed fetch) Unavailable overlay appears
- ? Message: "Unable to connect to tracking service..."
- ? Retry button visible

**Then:**
1. Passenger device: Re-enable network
2. Tap "Retry Now" button

**Expected After Network Returns:**
- ? Loading overlay appears briefly
- ? New location fetched
- ? Driver marker updates
- ? Status chip shows "Live" (gold)
- ? Unavailable overlay disappears

---

### ? TC007: Driver Completes Ride

**Preconditions:**
- Passenger is viewing live tracking
- Driver is en route

**Steps:**
1. Driver app: Arrive at destination
2. Driver app: Tap "Complete Ride"
3. Passenger app: Continue viewing tracking page

**Expected Results:**
- ? Next poll returns `TrackingActive: false` or 404
- ? Status chip changes to "Ended" (gray)
- ? ETA label shows "Ride ended"
- ? Distance label clears
- ? Driver marker remains visible (optional)
- ? Polling continues (or stops, depending on implementation)

**Alternative:**
If backend sends `CurrentStatus: "Completed"`:
- ? State changes to `TrackingState.Ended`

---

### ? TC008: Rapid State Changes

**Preconditions:**
- Passenger viewing tracking

**Steps:**
1. Tracking not started ? Driver starts ? GPS lost ? GPS restored ? Ride completes

**Expected Results:**
- ? NotStarted ? Tracking ? Unavailable ? Tracking ? Ended
- ? Each state transition shows correct UI
- ? No visual glitches or race conditions
- ? Status chip color changes appropriately

---

### ? TC009: ETA Accuracy

**Preconditions:**
- Driver 10 km away from pickup
- Driver traveling at ~50 km/h

**Steps:**
1. View tracking page
2. Observe ETA calculation

**Expected Results:**
- ? ETA shows approximately 12 minutes (10 km / 50 km/h * 60 = 12 min)
- ? Distance shows "10.0 km away"
- ? As driver approaches, ETA decreases
- ? When < 100m away, ETA shows "Arriving now"

**Formula Verification:**
```
ETA (minutes) = (Distance km / Speed km/h) * 60
```

If driver speed unavailable:
```
ETA (minutes) = (Distance km / 35 km/h) * 60
Label shows "(est.)"
```

---

### ? TC010: Map Auto-Zoom

**Preconditions:**
- Driver 5 km away from pickup

**Steps:**
1. Open tracking page
2. Observe initial map zoom
3. Wait for driver location update

**Expected Results:**
- ? Initial view shows pickup pin
- ? When driver location received, map zooms to show both pins
- ? Zoom level adjusts based on distance:
  - Driver close (< 1 km) ? zoomed in
  - Driver far (> 5 km) ? zoomed out
- ? Both pins always visible
- ? Map re-centers when driver moves significantly

---

### ? TC011: Stale Location Warning

**Preconditions:**
- Tracking active

**Steps:**
1. Observe location updates
2. Driver stops sending updates for > 2 minutes

**Expected Results:**
- ? First 120 seconds: No warning
- ? After 120 seconds: Stale warning appears
- ? Pin address changes to "Last known location"
- ? Warning text: "? Location may be outdated"

**Console:**
```
[DriverTrackingService] Location age: 125s (stale)
```

---

### ? TC012: Retry Button Functionality

**Preconditions:**
- Unavailable overlay visible
- Retry button enabled

**Steps:**
1. Tap "Retry Now" button

**Expected Results:**
- ? Unavailable overlay hides
- ? Loading overlay shows
- ? New API request sent immediately
- ? If location available ? transitions to Tracking
- ? If still unavailable ? unavailable overlay shows again

---

### ? TC013: Close Tracking Page

**Preconditions:**
- Tracking page open

**Steps:**
1. Tap "Close" toolbar button
2. OR use device back button

**Expected Results:**
- ? Polling stops immediately
- ? Navigation returns to booking detail page
- ? No memory leaks
- ? Event handlers unsubscribed

**Console:**
```
[DriverTrackingService] Tracking stopped
[DriverTrackingService] Polling loop ended
```

---

### ? TC014: Multiple Bookings

**Preconditions:**
- Alice has 2 active bookings:
  - Booking A: Driver en route
  - Booking B: Driver not started

**Steps:**
1. View tracking for Booking A
2. Navigate back
3. View tracking for Booking B
4. Navigate back
5. View tracking for Booking A again

**Expected Results:**
- ? Booking A shows live tracking
- ? Booking B shows "not started" message
- ? Returning to Booking A restarts tracking
- ? No data from Booking B bleeds into Booking A
- ? Each session is independent

---

## Performance Testing

### ? PT001: Battery Usage

**Steps:**
1. Fully charge device
2. Open tracking page
3. Leave tracking active for 30 minutes
4. Measure battery drain

**Expected:**
- ? Battery drain < 10% in 30 minutes
- ? Polling every 15 seconds (4 requests/min)
- ? No runaway processes

---

### ? PT002: Data Usage

**Steps:**
1. Reset network statistics
2. Track driver for 30 minutes
3. Measure data usage

**Expected:**
- ? ~2 KB per request * 120 requests = ~240 KB in 30 minutes
- ? Total data usage < 500 KB

---

### ? PT003: Memory Leaks

**Steps:**
1. Open tracking page
2. Close tracking page
3. Repeat 10 times
4. Check memory usage

**Expected:**
- ? Memory usage doesn't increase significantly
- ? Event handlers properly unsubscribed
- ? No retained references to disposed objects

---

## Edge Cases

### ?? EC001: Negative Coordinates

**Test Data:**
- Pickup: -33.8688, 151.2093 (Sydney)
- Driver: -33.8700, 151.2100

**Expected:**
- ? Map displays correctly
- ? Distance calculated correctly
- ? ETA calculated correctly

---

### ?? EC002: International Date Line

**Test Data:**
- Pickup: 64.8378, -147.7164 (Alaska)
- Driver: 64.8400, 179.9999 (near date line)

**Expected:**
- ? Map wraps correctly
- ? Distance calculated correctly (not across entire globe)

---

### ?? EC003: Very Close Distance

**Test Data:**
- Pickup: 41.8781, -87.6298
- Driver: 41.8782, -87.6299 (< 100m away)

**Expected:**
- ? ETA shows "Arriving now"
- ? Distance shows "50 meters away" (not km)
- ? Map zoomed in appropriately

---

### ?? EC004: Very Far Distance

**Test Data:**
- Pickup: 41.8781, -87.6298 (Chicago)
- Driver: 40.7128, -74.0060 (New York, ~1100 km away)

**Expected:**
- ? ETA calculated (may be unrealistic, e.g., 31 hours)
- ? Distance shows "1100.0 km away"
- ? Map zooms out to show both locations

---

### ?? EC005: Zero Speed

**Test Data:**
- Driver location updates with `speed: 0` or `speed: null`

**Expected:**
- ? ETA calculated using default speed (35 km/h)
- ? Label shows "(est.)" to indicate estimate

---

## Regression Testing

### ?? RT001: Booking Detail Page

**Verify:**
- ? "Track Driver" button still appears for trackable statuses
- ? Button hidden for non-trackable statuses
- ? Navigation to tracking page works
- ? Back navigation returns to detail page

---

### ?? RT002: Bookings List

**Verify:**
- ? List loads correctly
- ? Status chips show correct colors
- ? CurrentRideStatus displayed when available
- ? Tapping booking opens detail page

---

### ?? RT003: Authentication

**Verify:**
- ? JWT token sent with location requests
- ? Expired token refreshed automatically
- ? Logout clears tracking session

---

## Acceptance Criteria

### ? Must Have
- [x] Passenger can track their own bookings
- [x] Unauthorized access prevented (403)
- [x] "Not started" message displays clearly
- [x] Live tracking updates every ~15 seconds
- [x] ETA and distance calculated
- [x] Map shows driver and pickup pins
- [x] Status chip reflects current state
- [x] Network errors handled gracefully
- [x] No crashes or freezes

### ?? Should Have
- [ ] SignalR real-time updates (future)
- [ ] Push notifications (future)
- [ ] Offline mode (show last known) (future)
- [ ] Route polyline on map (future)

### ?? Nice to Have
- [ ] Driver photo and vehicle info
- [ ] Call driver button
- [ ] Share ETA with others
- [ ] Historical route playback

---

## Sign-Off Checklist

**Functional Testing:**
- [ ] All 14 test cases pass
- [ ] All 5 edge cases handled
- [ ] All 3 regression tests pass

**Performance Testing:**
- [ ] Battery usage acceptable
- [ ] Data usage acceptable
- [ ] No memory leaks

**Security Testing:**
- [ ] Unauthorized access blocked
- [ ] JWT tokens validated
- [ ] No sensitive data leaked

**UX Testing:**
- [ ] Error messages clear and helpful
- [ ] Loading states smooth
- [ ] Retry button works
- [ ] Navigation intuitive

**Code Quality:**
- [ ] Build successful
- [ ] No compiler warnings
- [ ] Debug logging in place
- [ ] Documentation complete

---

## Known Issues

### ?? Issue #1: Polling Delay
**Description:** Up to 15-second delay before updates appear

**Workaround:** Reduce polling interval or implement SignalR

**Planned Fix:** SignalR implementation in v2.1

---

### ?? Issue #2: ETA Accuracy
**Description:** ETA uses straight-line distance, not road distance

**Workaround:** Label shows "(est.)" to indicate estimate

**Planned Fix:** Integrate Google Maps Directions API in v2.2

---

## Support

**Logs Location:**
- iOS: Xcode Console
- Android: Logcat
- Windows: Output Window ? Debug

**Search for:**
```
[DriverTrackingService]
[DriverTrackingPage]
```

**Common Issues:**
1. **403 Forbidden** ? Check backend deployment
2. **401 Unauthorized** ? Refresh JWT token
3. **Stuck on Loading** ? Check network connectivity
4. **No map visible** ? Verify MapView permissions

---

**Version:** 2.0.0  
**Date:** December 2024  
**Status:** ? READY FOR QA  
**Estimated Testing Time:** 4-6 hours
