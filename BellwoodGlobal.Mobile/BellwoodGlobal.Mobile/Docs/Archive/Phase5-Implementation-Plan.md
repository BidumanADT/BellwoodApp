# Phase 5 Implementation Plan - Lifecycle + Resilience Hardening

**Date:** December 30, 2025  
**Status:** ? **READY TO START**  
**Branch:** `feature/maps-address-autocomplete-phase5`  

---

## Goal

Make the Quote and Booking flows **unbreakable** even when:
- User switches apps (OS suspends the app)
- User receives a phone call during booking
- App is killed by OS (memory pressure)
- User closes and reopens the app mid-form

**User Experience:** Never lose form progress, coordinates stay preserved, autocomplete state recovers gracefully.

---

## Scope

### In Scope ✅
- Persist form state for `QuotePage` and `BookRidePage`
- Restore form state on `OnAppearing`
- Persist autocomplete coordinates
- Handle app suspension/resume
- Handle app termination/restart
- Offer "restore draft" prompt if form was in progress

### Out of Scope ❌
- Multi-device sync (future: cloud-backed drafts)
- Auto-save to backend (that's submission, not persistence)
- Persistent undo/redo (overkill for this phase)

---

## Technical Approach

### Storage Mechanism: `Preferences` (MAUI)

**Why Preferences?**
- ✅ Built into .NET MAUI
- ✅ Persistent across app restarts
- ✅ Survives app suspension
- ✅ Platform-agnostic (Android, iOS, Windows)
- ✅ Simple key-value API
- ✅ Supports strings (we'll use JSON)

**Why Not SecureStorage?**
- Form data isn't sensitive (no PII, no payment info)
- Booker info is read-only (from profile)
- Passenger info is optional (user can re-enter)

**Why Not File System?**
- Overkill for this use case
- Preferences is simpler and more reliable

---

## What to Persist

### QuotePage State

**Core Fields:**
```json
{
  "pickupLocationIndex": 2,
  "pickupNewLabel": "JFK Airport",
  "pickupNewAddress": "Jamaica, NY 11430",
  "pickupLatitude": 40.6413,
  "pickupLongitude": -73.7781,
  "pickupPlaceId": "ChIJR0lA1VBmwokR8BGfSBOyT-w",
  
  "dropoffSelection": "New Location",
  "dropoffNewLabel": "Times Square",
  "dropoffNewAddress": "Manhattan, NY 10036",
  "dropoffLatitude": 40.758896,
  "dropoffLongitude": -73.985130,
  "dropoffPlaceId": "ChIJmQJIxlVYwokRLgeuocVOGVU",
  
  "pickupDateTime": "2025-01-15T14:30:00",
  "vehicleClass": "SUV",
  "passengerIndex": 0,
  "passengerFirstName": "Alice",
  "passengerLastName": "Morgan",
  
  "roundTrip": true,
  "returnDateTime": "2025-01-17T09:00:00",
  
  "autocompleteSearchText_Pickup": "JFK",
  "autocompleteSearchText_Dropoff": "Times",
  
  "lastModified": "2025-12-30T15:30:00Z"
}
```

**What NOT to Persist:**
- Booker info (read-only from profile)
- Saved passengers/locations lists (from profile)
- Capacity banners (computed on the fly)
- JSON preview (generated on submit)

---

### BookRidePage State

**Additional Fields:**
```json
{
  // ... all QuotePage fields ...
  "paymentMethodIndex": 0,
  "paymentMethodId": "pm_abc123",
  "newCardHolderName": "", // Empty if not adding new card
  "newCardNumber": "",     // Never persist full number!
  "lastModified": "2025-12-30T15:30:00Z"
}
```

**Security Note:** Never persist full credit card numbers or CVCs! Only persist:
- Selected payment method index
- Cardholder name (if entering new card)
- Partial card number (last 4 digits) for display only

---

## Implementation Components

### 1. Form State Service (`IFormStateService`)

**Purpose:** Centralized persistence logic, reusable across pages

**Interface:**
```csharp
public interface IFormStateService
{
    Task SaveQuoteFormStateAsync(QuoteFormState state);
    Task<QuoteFormState?> LoadQuoteFormStateAsync();
    Task ClearQuoteFormStateAsync();
    
    Task SaveBookingFormStateAsync(BookingFormState state);
    Task<BookingFormState?> LoadBookingFormStateAsync();
    Task ClearBookingFormStateAsync();
    
    bool HasSavedQuoteForm();
    bool HasSavedBookingForm();
}
```

**Implementation:**
```csharp
public class FormStateService : IFormStateService
{
    private const string QuoteKey = "QuotePage_State";
    private const string BookingKey = "BookRidePage_State";
    
    public async Task SaveQuoteFormStateAsync(QuoteFormState state)
    {
        var json = JsonSerializer.Serialize(state);
        Preferences.Set(QuoteKey, json);
    }
    
    // ... etc
}
```

---

### 2. Form State Models

**QuoteFormState:**
```csharp
public class QuoteFormState
{
    // Pickup
    public int? PickupLocationIndex { get; set; }
    public string? PickupNewLabel { get; set; }
    public string? PickupNewAddress { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? PickupPlaceId { get; set; }
    
    // Dropoff
    public string? DropoffSelection { get; set; } // "As Directed" | "New Location" | saved location
    public string? DropoffNewLabel { get; set; }
    public string? DropoffNewAddress { get; set; }
    public double? DropoffLatitude { get; set; }
    public double? DropoffLongitude { get; set; }
    public string? DropoffPlaceId { get; set; }
    
    // Date/Time
    public DateTime? PickupDateTime { get; set; }
    public DateTime? ReturnDateTime { get; set; }
    
    // Vehicle & Passenger
    public string? VehicleClass { get; set; }
    public int? PassengerIndex { get; set; }
    public string? PassengerFirstName { get; set; }
    public string? PassengerLastName { get; set; }
    
    // Round Trip
    public bool RoundTrip { get; set; }
    
    // Autocomplete in-progress text (nice-to-have)
    public string? AutocompleteSearchText_Pickup { get; set; }
    public string? AutocompleteSearchText_Dropoff { get; set; }
    
    // Metadata
    public DateTime LastModified { get; set; }
}
```

**BookingFormState:** (extends QuoteFormState)
```csharp
public class BookingFormState : QuoteFormState
{
    public int? PaymentMethodIndex { get; set; }
    public string? NewCardHolderName { get; set; }
    // Never store full card number or CVC!
}
```

---

### 3. Page Lifecycle Hooks

**QuotePage Updates:**

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    
    // Check for saved state
    if (_formStateService.HasSavedQuoteForm())
    {
        var shouldRestore = await DisplayAlert(
            "Restore Draft?",
            "You have an unsaved quote. Would you like to continue where you left off?",
            "Yes, Restore",
            "No, Start Fresh"
        );
        
        if (shouldRestore)
        {
            await RestoreFormStateAsync();
        }
        else
        {
            await _formStateService.ClearQuoteFormStateAsync();
        }
    }
}

protected override void OnDisappearing()
{
    base.OnDisappearing();
    
    // Auto-save form state
    _ = SaveFormStateAsync();
}

private async Task SaveFormStateAsync()
{
    var state = new QuoteFormState
    {
        PickupLocationIndex = PickupLocationPicker.SelectedIndex,
        PickupNewLabel = PickupNewLabel.Text,
        PickupNewAddress = PickupNewAddress.Text,
        PickupLatitude = _selectedPickupLocation?.Latitude,
        PickupLongitude = _selectedPickupLocation?.Longitude,
        PickupPlaceId = _selectedPickupLocation?.PlaceId,
        
        // ... capture all fields ...
        
        LastModified = DateTime.UtcNow
    };
    
    await _formStateService.SaveQuoteFormStateAsync(state);
}

private async Task RestoreFormStateAsync()
{
    var state = await _formStateService.LoadQuoteFormStateAsync();
    if (state == null) return;
    
    // Restore pickers
    if (state.PickupLocationIndex.HasValue)
        PickupLocationPicker.SelectedIndex = state.PickupLocationIndex.Value;
    
    // Restore text fields
    PickupNewLabel.Text = state.PickupNewLabel;
    PickupNewAddress.Text = state.PickupNewAddress;
    
    // Restore coordinates
    if (state.PickupLatitude.HasValue && state.PickupLongitude.HasValue)
    {
        _selectedPickupLocation = new Models.Location
        {
            Label = state.PickupNewLabel ?? "",
            Address = state.PickupNewAddress ?? "",
            Latitude = state.PickupLatitude.Value,
            Longitude = state.PickupLongitude.Value,
            PlaceId = state.PickupPlaceId,
            IsVerified = true
        };
    }
    
    // ... restore all fields ...
    
    // Show confirmation
    await DisplayAlert("Draft Restored", "Your quote has been restored.", "OK");
}
```

---

## Fallback Modes

### Scenario 1: Places API Down

**Detection:**
- `PlacesAutocompleteService` returns empty results
- Error logged in console
- Quota limit hit (tracked in preferences)

**Fallback:**
- Manual entry still works ✅
- "Pick from Maps" button still works ✅
- User can complete form without autocomplete

**UX:**
```
? Autocomplete shows: "Address search temporarily unavailable. Please enter manually."
? Manual entry fields remain visible
? No blocking errors
```

---

### Scenario 2: Quota Exceeded

**Detection:**
- Quota limit reached (tracked in `PlacesAutocompleteService`)
- 429 response from Google API

**Fallback:**
- Autocomplete component shows: "Daily address search limit reached. Please enter manually."
- Component disables itself
- Manual entry remains available

**Auto-Recovery:**
- Quota resets at midnight UTC
- Component re-enables automatically

---

### Scenario 3: App Killed Mid-Entry

**User Flow:**
1. User starts filling out quote
2. User types "JFK" in pickup autocomplete
3. OS kills app (memory pressure)
4. User reopens app
5. User navigates to QuotePage

**Expected Behavior:**
- Prompt: "Restore Draft?"
- If Yes:
  - Form fields populated
  - Pickup autocomplete shows "JFK" (in-progress text)
  - User can continue typing or select from predictions
- If No:
  - Fresh form
  - Saved state cleared

---

## Testing Scenarios

### Test 1: Basic Persistence

**Steps:**
1. Open QuotePage
2. Select pickup: "JFK Airport" (via autocomplete)
3. Select dropoff: "Times Square" (via autocomplete)
4. Tap home button (app suspended)
5. Reopen app
6. Navigate to QuotePage

**Expected:**
- ✅ Prompt: "Restore Draft?"
- ✅ If Yes: Both locations restored with coordinates
- ✅ If No: Fresh form

---

### Test 2: In-Progress Autocomplete

**Steps:**
1. Open QuotePage
2. Type "123 Main" in pickup autocomplete (don't select)
3. Close app (OS kills it)
4. Reopen app
5. Navigate to QuotePage
6. Restore draft

**Expected:**
- ✅ Autocomplete search field shows "123 Main"
- ✅ User can continue typing or backspace

---

### Test 3: Coordinates Preservation

**Steps:**
1. Open QuotePage
2. Select "JFK Airport" via autocomplete
3. Verify coordinates captured: 40.6413, -73.7781
4. Close app
5. Reopen app
6. Restore draft
7. Submit quote

**Expected:**
- ✅ Draft includes `PickupLatitude` and `PickupLongitude`
- ✅ Backend receives coordinates

---

### Test 4: Manual Entry After Restore

**Steps:**
1. Save draft with autocomplete locations
2. Close app
3. Reopen, restore draft
4. Change pickup to manual entry
5. Type new address manually

**Expected:**
- ✅ Can switch to manual entry
- ✅ Previous coordinates cleared
- ✅ New manual address saved

---

### Test 5: Places API Down

**Steps:**
1. Simulate API failure (turn off WiFi or set quota to 0)
2. Try to use autocomplete

**Expected:**
- ✅ Error message shown
- ✅ Manual entry still works
- ✅ Can complete form without autocomplete

---

## Acceptance Criteria

### PAC-5.1: Form State Persistence ✅
- [ ] Quote form state saved to `Preferences` on `OnDisappearing`
- [ ] Booking form state saved to `Preferences` on `OnDisappearing`
- [ ] State includes all user-entered fields
- [ ] State includes autocomplete coordinates

### PAC-5.2: State Restoration ✅
- [ ] "Restore Draft?" prompt shown on `OnAppearing` if state exists
- [ ] User can choose to restore or start fresh
- [ ] All fields repopulate correctly
- [ ] Coordinates preserved and restored

### PAC-5.3: App Suspension ✅
- [ ] Switching apps doesn't wipe form
- [ ] Returning to app preserves progress
- [ ] No data loss on suspension

### PAC-5.4: App Termination ✅
- [ ] App killed by OS → state persists
- [ ] Reopening app offers restore
- [ ] Coordinates survive termination

### PAC-5.5: Manual Entry Fallback ✅
- [ ] Manual entry always available
- [ ] Works when Places API down
- [ ] Works when quota exceeded
- [ ] No blocking errors

### PAC-5.6: Autocomplete Graceful Degradation ✅
- [ ] API error → user-friendly message
- [ ] Quota exceeded → disable gracefully
- [ ] Network failure → fallback to manual
- [ ] No crashes on any error scenario

---

## Implementation Checklist

### Step 1: Create Models ✅
- [ ] `Models/QuoteFormState.cs`
- [ ] `Models/BookingFormState.cs`

### Step 2: Create Service ✅
- [ ] `Services/IFormStateService.cs`
- [ ] `Services/FormStateService.cs`
- [ ] Register in `MauiProgram.cs`

### Step 3: Update QuotePage ✅
- [ ] Add `OnDisappearing` → Save state
- [ ] Add `OnAppearing` → Check for saved state
- [ ] Add `RestoreFormStateAsync()` method
- [ ] Add "Restore Draft?" dialog
- [ ] Clear state on successful submission

### Step 4: Update BookRidePage ✅
- [ ] Same as QuotePage
- [ ] Handle payment method persistence (index only)
- [ ] Never persist card numbers/CVCs

### Step 5: Update PlacesAutocompleteService ✅
- [ ] Add "fallback mode" flag
- [ ] Better error messages
- [ ] Graceful degradation on quota/errors

### Step 6: Update LocationAutocompleteView ✅
- [ ] Show error state clearly
- [ ] Disable component on quota exceeded
- [ ] Preserve search text on restore

### Step 7: Testing ✅
- [ ] Test all scenarios above
- [ ] Verify on Android emulator
- [ ] Verify on iOS simulator (if available)
- [ ] Test quota exhaustion scenario

---

## Build & Deploy

### Build Status
- [ ] Compiles without errors
- [ ] 0 warnings (or only XAML binding warnings)

### Manual Testing
- [ ] Android emulator: All scenarios pass
- [ ] iOS simulator: All scenarios pass
- [ ] Physical device: All scenarios pass

---

## Next Steps

**After Phase 5:**
- Phase 6: Comprehensive testing & validation
- Production deployment
- Feature flag rollout
- Monitoring & metrics

---

**Status:** ? **READY TO IMPLEMENT**  
**Estimated Time:** 3-4 hours  
**Risk:** Low (using battle-tested Preferences API)