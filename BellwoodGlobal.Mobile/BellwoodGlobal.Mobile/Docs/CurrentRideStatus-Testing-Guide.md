# CurrentRideStatus Testing Guide

## Pre-Test Setup

### Backend Verification
1. Ensure the backend API `/bookings/list` endpoint returns `CurrentRideStatus`
2. Verify driver app can update ride status via `/driver/rides/{rideId}/status`
3. Confirm SignalR hub broadcasts `RideStatusChanged` events

### Test Environment
- Driver app running on one device/emulator
- Passenger app running on another device/emulator
- Both connected to same backend instance
- At least one confirmed booking scheduled for testing

## Test Scenarios

### Scenario 1: Default Booking State
**Test**: View booking before driver assignment
- [ ] Open passenger app bookings list
- [ ] Verify booking shows "Scheduled" status
- [ ] Verify status chip is green (SeaGreen)
- [ ] Open booking detail
- [ ] Verify "Track Driver" banner is NOT visible

**Expected API Response**:
```json
{
  "status": "Scheduled",
  "currentRideStatus": null
}
```

---

### Scenario 2: Driver Starts Ride (OnRoute)
**Test**: Driver changes status to OnRoute
1. Driver app: Select the ride
2. Driver app: Tap "Start Ride" ? status changes to OnRoute
3. Passenger app: Refresh bookings list (or wait for real-time update)

**Verification Steps**:
- [ ] Bookings list shows "Driver En Route"
- [ ] Status chip color is gold (BellwoodGold)
- [ ] Open booking detail
- [ ] Verify "Track Driver" banner IS visible
- [ ] Tap "Track Driver" ? opens map with driver location

**Expected API Response**:
```json
{
  "status": "Scheduled",
  "currentRideStatus": "OnRoute"
}
```

**Console Output** (Driver App):
```
[DriverApp] Status changed to OnRoute
[API] POST /driver/rides/{rideId}/status ? 200 OK
[SignalR] RideStatusChanged broadcast: {rideId}, OnRoute
```

---

### Scenario 3: Driver Arrives at Pickup
**Test**: Driver reaches pickup location
1. Driver app: Tap "Arrived at Pickup"
2. Passenger app: Refresh or observe real-time update

**Verification Steps**:
- [ ] Bookings list shows "Driver Arrived"
- [ ] Status chip remains gold
- [ ] "Track Driver" banner visible
- [ ] Map shows driver at pickup location

**Expected API Response**:
```json
{
  "status": "Scheduled",
  "currentRideStatus": "Arrived"
}
```

---

### Scenario 4: Passenger Boards
**Test**: Passenger enters vehicle
1. Driver app: Tap "Passenger On Board"
2. Passenger app: Refresh or observe update

**Verification Steps**:
- [ ] Bookings list shows "Passenger On Board"
- [ ] Status chip is gold
- [ ] "Track Driver" banner visible
- [ ] Can track route to destination

**Expected API Response**:
```json
{
  "status": "Scheduled",
  "currentRideStatus": "PassengerOnboard"
}
```

---

### Scenario 5: Ride Completion
**Test**: Driver completes ride
1. Driver app: Tap "Complete Ride"
2. Passenger app: Refresh

**Verification Steps**:
- [ ] Bookings list shows "Completed"
- [ ] Status chip is gray (LightGray)
- [ ] "Track Driver" banner NOT visible
- [ ] Booking remains in history

**Expected API Response**:
```json
{
  "status": "Completed",
  "currentRideStatus": "Completed"
}
```

---

### Scenario 6: Multiple Bookings Mixed States
**Test**: View bookings list with various statuses
1. Create multiple test bookings
2. Have drivers update different bookings to different statuses
3. View passenger app bookings list

**Verification Steps**:
- [ ] Booking A: "Scheduled" (gray)
- [ ] Booking B: "Driver En Route" (gold)
- [ ] Booking C: "Driver Arrived" (gold)
- [ ] Booking D: "Passenger On Board" (gold)
- [ ] Booking E: "Completed" (gray)
- [ ] Each status chip has correct color
- [ ] List is sorted correctly (usually by pickup time)

---

## Edge Cases

### Edge Case 1: Status Update Failure
**Scenario**: Driver loses network during status update
1. Driver app: Turn off network
2. Driver app: Try to change status
3. Driver app: Reconnect network

**Expected Behavior**:
- [ ] Driver app shows error message
- [ ] Status change is queued or retried
- [ ] Passenger app maintains last known status
- [ ] No crash or UI corruption

---

### Edge Case 2: Backwards Compatibility
**Scenario**: Backend doesn't return CurrentRideStatus
**API Response**:
```json
{
  "status": "Scheduled"
  // No currentRideStatus field
}
```

**Expected Behavior**:
- [ ] Passenger app doesn't crash
- [ ] Shows booking-level status ("Scheduled")
- [ ] Status chip shows appropriate color
- [ ] No null reference exceptions

---

### Edge Case 3: Unknown Status Value
**Scenario**: Backend returns unexpected status
**API Response**:
```json
{
  "status": "Scheduled",
  "currentRideStatus": "UnknownStatus"
}
```

**Expected Behavior**:
- [ ] App displays raw status value
- [ ] Uses default color (gray)
- [ ] No crash
- [ ] Logs warning in debug mode

---

## Performance Testing

### Load Test: 50+ Bookings
1. Create 50+ test bookings with various statuses
2. Open passenger app bookings list
3. Scroll through list

**Verification**:
- [ ] List loads in < 2 seconds
- [ ] Scrolling is smooth (60 fps)
- [ ] Status chips render correctly
- [ ] No memory leaks

---

## Regression Testing

### Previously Working Features
- [ ] Booking creation still works
- [ ] Booking cancellation still works
- [ ] Search/filter functionality works
- [ ] Booking detail view loads correctly
- [ ] Driver tracking opens when tapped
- [ ] Back navigation works

---

## Debug Verification

### Enable Debug Logging
Set `#if DEBUG` flag and check console output:

**Expected Logs**:
```
[AdminApi] /bookings/list ? 5 items
[AdminApi] First: Id=booking-123, Status=Scheduled, CurrentRideStatus=OnRoute
[BookingsPage] Effective status for booking-123: Driver En Route
[BookingDetailPage] Status: OnRoute ? Display: Driver En Route
```

---

## Sign-Off Checklist

**Functional Tests**:
- [ ] All 6 main scenarios pass
- [ ] All 3 edge cases handled correctly
- [ ] Performance is acceptable
- [ ] No regressions

**Code Quality**:
- [ ] Build succeeds without warnings
- [ ] No null reference exceptions
- [ ] Proper error handling
- [ ] Debug logging in place

**Documentation**:
- [ ] Implementation docs created
- [ ] Testing guide created
- [ ] Backend requirements documented
- [ ] Future enhancements noted

**Deployment Ready**:
- [ ] Code reviewed
- [ ] Tests passed
- [ ] Documentation complete
- [ ] Backend API updated

---

## Troubleshooting

### Issue: Status still shows "Scheduled"
**Possible Causes**:
1. Backend not returning `CurrentRideStatus`
2. API response format mismatch
3. Passenger app not refreshing

**Solutions**:
1. Check API response in network inspector
2. Verify JSON deserialization
3. Force refresh by pulling down on list

### Issue: "Track Driver" banner not appearing
**Possible Causes**:
1. `CurrentRideStatus` is null
2. Status value not in trackable list
3. UI binding issue

**Solutions**:
1. Verify `IsTrackableStatus()` includes the status
2. Check `TrackDriverBanner.IsVisible` in debugger
3. Ensure `CurrentRideStatus` is populated

### Issue: Wrong color on status chip
**Possible Causes**:
1. Status mapping incorrect
2. Color resource not found
3. Display name mismatch

**Solutions**:
1. Verify `StatusColorForDisplay()` mappings
2. Check `App.xaml` for color resources
3. Use exact display names in switch statement

---

**Last Updated**: 2024  
**Version**: 1.0  
**Status**: ? Ready for Testing
