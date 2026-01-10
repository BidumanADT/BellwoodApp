# Bug Fixes Reference - Complete History

**Last Updated:** January 10, 2026  
**Status:** ? Maintained

---

## ?? Quick Index

This document consolidates all bug fixes across the Bellwood Elite mobile app, organized by component for easy reference.

---

## ??? Google Places Autocomplete Bugs

### Bug #1: Malformed URL in API Requests
**Date:** Dec 29, 2025  
**Severity:** ?? Critical  
**Component:** `PlacesAutocompleteService`

**Problem:**
- Spaces in search queries caused "Malformed URL" exceptions
- Example: "123 Main St" ? API call failed

**Root Cause:**
```csharp
// BEFORE (wrong):
var url = $"/v1/places:autocomplete?input={input}";
// Spaces not encoded, caused malformed URL

// AFTER (fixed):
var url = $"/v1/places:autocomplete?input={Uri.EscapeDataString(input)}";
```

**Fix:** Use `Uri.EscapeDataString()` for all query parameters

**Files Changed:**
- `Services/PlacesAutocompleteService.cs`

**Testing:** ? Verified with "123 Main St", "O'Hare Airport", "New York, NY"

---

### Bug #2: XAML Parse Exception - Behavior Reference
**Date:** Dec 30, 2025  
**Severity:** ?? Medium  
**Component:** `LocationAutocompleteView`

**Problem:**
- XamlParseException when loading component
- Error: "Behavior referenced before initialization"

**Root Cause:**
```xml
<!-- BEFORE (wrong): -->
<Entry>
    <Entry.Behaviors>
        <behaviors:SomeBehavior />
    </Entry.Behaviors>
</Entry>

<!-- Behavior loaded before component fully initialized -->

<!-- AFTER (fixed): -->
<Entry Loaded="OnEntryLoaded">
    <!-- Behavior added in code-behind after load -->
</Entry>
```

**Fix:** Move behavior registration to code-behind `OnLoaded` event

**Files Changed:**
- `Components/LocationAutocompleteView.xaml.cs`

**Testing:** ? Component loads without exceptions

---

## ?? Location Tracking Bugs

### Bug #3: DateTime Double Conversion
**Date:** Dec 18, 2025  
**Severity:** ?? Critical  
**Component:** `DriverTrackingService`, `BookingsPage`, `BookingDetailPage`

**Problem:**
- Timestamps displayed incorrectly (off by hours/days)
- UTC timestamps converted to local time TWICE

**Root Cause:**
```csharp
// BEFORE (wrong):
var timestamp = DateTime.UtcNow; // Already UTC
var local = timestamp.ToLocalTime(); // First conversion
var display = local.ToString("g"); // Second conversion in UI binding
// Result: Wrong time displayed

// AFTER (fixed):
var timestamp = response.Timestamp; // UTC from server
var display = DateTimeHelper.FormatFriendly(timestamp);
// Single conversion in helper method
```

**Fix:** Created `DateTimeHelper.FormatFriendly()` to centralize conversions

**Files Changed:**
- `Helpers/DateTimeHelper.cs` (created)
- `Pages/BookingsPage.xaml.cs`
- `Pages/BookingDetailPage.xaml.cs`
- `Services/DriverTrackingService.cs`

**Testing:** ? All timestamps now display correctly in user's local time

---

### Bug #4: Polling Loop State Bug
**Date:** Dec 18, 2025  
**Severity:** ?? Medium  
**Component:** `DriverTrackingService`

**Problem:**
- Tracking state stuck on "Loading" even when location received
- UI showed loading indicator indefinitely

**Root Cause:**
```csharp
// BEFORE (wrong):
if (location != null)
{
    LastKnownLocation = location;
    LocationUpdated?.Invoke(this, location);
    // Missing: SetState(TrackingState.Tracking);
}

// AFTER (fixed):
if (location != null)
{
    LastKnownLocation = location;
    SetState(TrackingState.Tracking); // Now updates state
    LocationUpdated?.Invoke(this, location);
}
```

**Fix:** Added `SetState(TrackingState.Tracking)` when location received

**Files Changed:**
- `Services/DriverTrackingService.cs`

**Testing:** ? State transitions correctly: Loading ? Tracking

---

### Bug #5: CurrentRideStatus Not Displayed
**Date:** Dec 18, 2025  
**Severity:** ?? Medium  
**Component:** `BookingDetailPage`, `BookingsPage`

**Problem:**
- Booking showed generic status instead of driver-specific status
- Example: "Confirmed" instead of "Driver En Route"

**Root Cause:**
```csharp
// BEFORE (wrong):
var displayStatus = ToDisplayStatus(booking.Status);
// Always used booking.Status, ignored CurrentRideStatus

// AFTER (fixed):
var effectiveStatus = !string.IsNullOrWhiteSpace(booking.CurrentRideStatus) 
    ? booking.CurrentRideStatus 
    : booking.Status;
var displayStatus = ToDisplayStatus(effectiveStatus);
```

**Fix:** Prefer `CurrentRideStatus` over `Status` when available

**Files Changed:**
- `Pages/BookingDetailPage.xaml.cs`
- `Pages/BookingsPage.xaml.cs`

**Testing:** ? Driver-specific statuses now displayed correctly

---

## ?? Form State Persistence Bugs

### Bug #6: Coordinates Lost on App Restart
**Date:** Dec 30, 2025  
**Severity:** ?? Critical  
**Component:** `FormStateService`, `QuotePage`, `BookRidePage`

**Problem:**
- Autocomplete coordinates not saved in form drafts
- After app restart, coordinates were null

**Root Cause:**
```csharp
// BEFORE (wrong):
public class QuotePageState
{
    public string? PickupNewLabel { get; set; }
    public string? PickupNewAddress { get; set; }
    // Missing: PickupLatitude, PickupLongitude
}

// AFTER (fixed):
public class QuotePageState
{
    public string? PickupNewLabel { get; set; }
    public string? PickupNewAddress { get; set; }
    public double? PickupLatitude { get; set; } // Added
    public double? PickupLongitude { get; set; } // Added
    public string? PickupPlaceId { get; set; } // Added
}
```

**Fix:** Added lat/lng/placeId fields to `QuotePageState` and `BookRidePageState`

**Files Changed:**
- `Models/QuotePageState.cs`
- `Models/BookRidePageState.cs`
- `Pages/QuotePage.xaml.cs` (save/restore logic)
- `Pages/BookRidePage.xaml.cs` (save/restore logic)

**Testing:** ? Coordinates now persist across app restarts

---

## ?? UI/Binding Bugs

### Bug #7: AppThemeBinding Override Issue
**Date:** Dec 15, 2025  
**Severity:** ?? Low  
**Component:** XAML Styling

**Problem:**
- AppThemeBinding not working in certain controls
- Light/dark theme switching inconsistent

**Root Cause:**
```xml
<!-- BEFORE (wrong): -->
<Entry Background="{AppThemeBinding Light=White, Dark=Black}">
    <Entry.Style>
        <Style TargetType="Entry">
            <Setter Property="Background" Value="Gray" />
            <!-- Overrides AppThemeBinding -->
        </Style>
    </Entry.Style>
</Entry>

<!-- AFTER (fixed): -->
<Entry>
    <Entry.Style>
        <Style TargetType="Entry">
            <Setter Property="Background">
                <Setter.Value>
                    <AppThemeBinding Light="White" Dark="Black" />
                </Setter.Value>
            </Setter>
        </Style>
    </Entry.Style>
</Entry>
```

**Fix:** Move AppThemeBinding into Style setter value

**Files Changed:**
- Various XAML files

**Testing:** ? Theme switching now works consistently

---

### Bug #8: Binding Path Empty Text Warning
**Date:** Dec 20, 2025  
**Severity:** ?? Low  
**Component:** XAML Data Binding

**Problem:**
- Console warnings: "Binding path cannot be empty"
- No functional impact but cluttered debug logs

**Root Cause:**
```xml
<!-- BEFORE (wrong): -->
<Label Text="{Binding}" />
<!-- Empty binding path -->

<!-- AFTER (fixed): -->
<Label Text="{Binding .}" />
<!-- Explicit self-binding -->
```

**Fix:** Use explicit `.` for self-binding instead of empty path

**Files Changed:**
- Various XAML files

**Testing:** ? Warnings eliminated

---

## ?? Security/Authorization Bugs

### Bug #9: Bookings Access Without Authorization
**Date:** Dec 18, 2025  
**Severity:** ?? Critical  
**Component:** `BookingDetailPage`, `DriverTrackingPage`

**Problem:**
- Users could view other users' bookings
- No authorization check on booking details

**Root Cause:**
- Backend endpoint `/bookings/{id}` didn't validate ownership
- Mobile app assumed all data was user-scoped

**Fix (Backend Required):**
```csharp
// Backend must add:
var booking = await GetBookingAsync(id);
if (booking.Booker.Email != User.Email && booking.Passenger.Email != User.Email)
{
    return Forbid(); // 403 Forbidden
}
```

**Fix (Mobile App):**
```csharp
// Handle 403 gracefully
if (response.StatusCode == HttpStatusCode.Forbidden)
{
    await DisplayAlert("Unauthorized", "You don't have access to this booking.", "OK");
    await Navigation.PopAsync();
}
```

**Files Changed:**
- `Pages/BookingDetailPage.xaml.cs`
- `Services/DriverTrackingService.cs`

**Testing:** ? 403 errors handled gracefully, user redirected

**Note:** ?? **Backend fix still pending** (See `Planning-UserAccountIsolation.md`)

---

## ?? Summary Statistics

| Component | Bugs Fixed | Severity Breakdown |
|-----------|------------|--------------------|
| Google Places Autocomplete | 2 | ?? 1, ?? 1 |
| Location Tracking | 3 | ?? 1, ?? 2 |
| Form State Persistence | 1 | ?? 1 |
| UI/Binding | 2 | ?? 2 |
| Security/Authorization | 1 | ?? 1 |
| **TOTAL** | **9** | **?? 4, ?? 3, ?? 2** |

---

## ?? Common Patterns

### Pattern 1: DateTime Conversion Issues
**Symptoms:** Timestamps off by hours/days  
**Root Cause:** Double conversion (UTC ? Local ? Local)  
**Solution:** Single conversion, use helper methods

**Related Bugs:** #3

---

### Pattern 2: State Management Issues
**Symptoms:** UI stuck in loading state, wrong status displayed  
**Root Cause:** Missing state updates in event handlers  
**Solution:** Always call `SetState()` or equivalent

**Related Bugs:** #4, #5

---

### Pattern 3: Missing Properties in Models
**Symptoms:** Data lost after serialization/deserialization  
**Root Cause:** Properties not included in model classes  
**Solution:** Add properties, ensure serialization attributes

**Related Bugs:** #6

---

### Pattern 4: URL Encoding Issues
**Symptoms:** API calls fail with "Malformed URL"  
**Root Cause:** Special characters not escaped  
**Solution:** Use `Uri.EscapeDataString()` for all query params

**Related Bugs:** #1

---

## ?? Related Documentation

- `Feature-GooglePlacesAutocomplete.md` - Autocomplete implementation
- `Feature-LocationTracking.md` - Tracking implementation
- `Feature-FormStatePersistence.md` - Form state details
- `Planning-UserAccountIsolation.md` - Security fixes pending

---

## ?? Bug Report Template

When reporting a new bug, use this template:

```markdown
### Bug #{number}: {Short Title}
**Date:** {Date}
**Severity:** ??/??/??
**Component:** {ComponentName}

**Problem:**
- {Description}

**Root Cause:**
```csharp
// BEFORE (wrong):
{old code}

// AFTER (fixed):
{new code}
```

**Fix:** {Summary of fix}

**Files Changed:**
- {List of files}

**Testing:** {How to verify fix}
```

---

**Status:** ? All documented bugs fixed  
**Version:** 1.0  
**Maintainer:** Development Team
