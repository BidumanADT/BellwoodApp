# Testing Guide - Location Tracking

**Feature:** Driver Location Tracking  
**Last Updated:** January 10, 2026  
**Status:** ? Complete

---

## ?? Test Objectives

Verify that real-time driver location tracking works correctly:
- ? Location updates
- ? ETA calculation
- ? Map display
- ? Authorization
- ? Error handling

---

## ?? Test Scenarios

### Test 1: Happy Path - Driver Tracking ?

**Preconditions:**
- Booking created as Alice
- Driver assigned
- Driver status = "OnRoute"
- Driver sending location updates

**Steps:**
1. Login as Alice
2. Navigate to Bookings
3. Tap on booking
4. Tap "Track Driver" banner
5. Wait 15 seconds
6. Observe map and ETA

**Expected Results:**
- ? Map loads with pickup pin (green)
- ? Driver marker appears (gold car icon)
- ? ETA displayed (e.g., "8 min away (3.21 km)")
- ? Status chip shows "Driver En Route" (gold)
- ? Location updates every 15 seconds
- ? ETA decreases as driver approaches

**Pass Criteria:** All elements display correctly, location updates continuously

---

### Test 2: Tracking Not Started ?

**Preconditions:**
- Booking created
- Driver assigned
- Driver status = "Confirmed" (hasn't started trip)

**Steps:**
1. Tap "Track Driver"
2. Wait 15 seconds
3. Observe loading overlay

**Expected Results:**
- ? Loading overlay shows
- ? Message: "Driver hasn't started trip yet"
- ? Status chip shows "Waiting" (orange)
- ? Polling continues every 15 seconds
- ? Map shows only pickup pin

**Pass Criteria:** Appropriate message shown, polling continues

---

### Test 3: Unauthorized Access ?

**Preconditions:**
- Booking A belongs to Alice
- Booking B belongs to Bob

**Steps:**
1. Login as Bob
2. Try to navigate to Booking A's tracking (manually enter URL)
3. Observe error

**Expected Results:**
- ? Unavailable overlay shows
- ? Message: "Not authorized to view this ride"
- ? Status chip shows "Error" (red)
- ? Polling stops
- ? Back button returns to bookings list

**Pass Criteria:** Unauthorized access blocked, graceful error message

---

### Test 4: Booking Not Found ?

**Preconditions:** None

**Steps:**
1. Navigate to tracking with invalid ride ID
2. Observe error

**Expected Results:**
- ? Unavailable overlay shows
- ? Message: "Ride not found"
- ? Status chip shows "Error" (red)
- ? Back button available

**Pass Criteria:** 404 handled gracefully

---

### Test 5: Network Error ?

**Preconditions:** None

**Steps:**
1. Start tracking
2. Turn off WiFi/data mid-tracking
3. Wait for next poll (15 sec)
4. Turn WiFi/data back on

**Expected Results:**
- ? Error overlay shows when offline
- ? Message: "Network error"
- ? Last known location preserved
- ? Tracking resumes when online
- ? No crash

**Pass Criteria:** Graceful offline handling, automatic recovery

---

### Test 6: ETA Calculation Accuracy ?

**Preconditions:**
- Driver location known
- Driver speed known (from GPS)

**Steps:**
1. Note driver's distance and speed
2. Calculate expected ETA manually
3. Compare with app's ETA

**Expected Results:**
- ? ETA within ±2 minutes of manual calculation
- ? Distance within ±0.5 km
- ? ETA updates as driver moves

**Pass Criteria:** ETA reasonably accurate

---

### Test 7: Multiple Status Transitions ?

**Preconditions:** Booking with driver

**Steps:**
1. Track driver while status = "Confirmed"
2. Wait for status ? "OnRoute"
3. Wait for status ? "Arrived"
4. Observe UI changes

**Expected Results:**
- ? "Confirmed" ? "Waiting" message
- ? "OnRoute" ? Driver marker appears + ETA
- ? "Arrived" ? ETA shows "0 min" or "Driver Arrived"
- ? Status chip color changes (orange ? gold ? green)

**Pass Criteria:** UI reflects status changes correctly

---

### Test 8: Map Interaction ?

**Preconditions:** Tracking active

**Steps:**
1. Zoom in on map
2. Pan map to different location
3. Wait for next location update
4. Observe behavior

**Expected Results:**
- ? Map zoom/pan works smoothly
- ? User's pan is preserved
- ? Driver marker updates in-place
- ? No automatic re-center (user control maintained)

**Pass Criteria:** Map controls responsive, driver updates work

---

### Test 9: App Backgrounding ?

**Preconditions:** Tracking active

**Steps:**
1. Press home button (background app)
2. Wait 30 seconds
3. Return to app

**Expected Results:**
- ? Location data refreshes on return
- ? ETA recalculated
- ? No crash

**Pass Criteria:** Tracking resumes correctly

---

### Test 10: Stop Tracking ?

**Preconditions:** Tracking active

**Steps:**
1. Tap back button
2. Navigate back to tracking

**Expected Results:**
- ? Polling stops when leaving page
- ? Polling restarts when returning
- ? Fresh location fetched
- ? No duplicate polling loops

**Pass Criteria:** Clean start/stop of tracking

---

## ?? Test Results Matrix

| Test # | Scenario | Android | iOS | Status |
|--------|----------|---------|-----|--------|
| 1 | Happy Path | ? | ? | Pass |
| 2 | Not Started | ? | ? | Pass |
| 3 | Unauthorized | ? | ? | Pass |
| 4 | Not Found | ? | ? | Pass |
| 5 | Network Error | ? | ? | Pass |
| 6 | ETA Accuracy | ? | ? | Pass |
| 7 | Status Transitions | ? | ? | Pass |
| 8 | Map Interaction | ? | ? | Pass |
| 9 | Backgrounding | ? | ? | Pass |
| 10 | Stop Tracking | ? | ? | Pass |

**Legend:**
- ? Tested & Passed
- ? Not Yet Tested
- ? Failed
- ?? Pass with Issues

---

## ?? Known Issues

### Issue 1: Polling Continues in Background (Low Priority)
**Description:** Polling continues when app is backgrounded  
**Impact:** Minor battery drain  
**Workaround:** None needed, polling stops when user navigates away  
**Fix:** Implement background/foreground detection

---

## ?? Debug Commands

**Watch tracking logs:**
```bash
adb logcat | grep "DriverTrackingService"
```

**Expected log patterns (success):**
```
[DriverTrackingService] START TRACKING CALLED
[DriverTrackingService] >>> HTTP Status: 200 OK
[DriverTrackingService] >>> TrackingActive: TRUE
[DriverTrackingService] Location received: 41.878100, -87.629800
[DriverTrackingService] ETA: 8 min away, Distance: 3.21 km
```

**Expected log patterns (not started):**
```
[DriverTrackingService] >>> TrackingActive: FALSE
[DriverTrackingService] >>> Message: Driver has not started tracking yet
[DriverTrackingService] State changed to: NotStarted
```

---

## ?? Regression Testing Checklist

Before each release, verify:
- [ ] Happy path tracking works
- [ ] Unauthorized access blocked (403)
- [ ] Network errors handled gracefully
- [ ] ETA calculation accurate
- [ ] Status transitions display correctly

---

## ?? Related Documents

- `Feature-LocationTracking.md` - Full implementation details
- `Reference-BugFixes.md` - DateTime fix, polling loop fix
- `Planning-UserAccountIsolation.md` - Authorization model

---

**Status:** ? All scenarios pass on Android  
**Version:** 1.0  
**Maintainer:** QA Team
