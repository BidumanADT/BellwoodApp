# How to Access PlacesTestPage (Phase 1 Testing)

**Date:** 2025-12-25  
**Phase:** 1 - Foundations Testing  

---

## Quick Access Instructions

### Method 1: Via Debug Button on MainPage (EASIEST) ?

1. **Run the app** (Android Emulator, iOS Simulator, or Windows)

2. **Log in** with your test credentials

3. **On the MainPage**, you'll see a new button:
   ```
   ?? Test Places API (Debug)
   ```
   *(It's a transparent button with gold border, below "Book a Ride")*

4. **Tap the button** ? You're now on `PlacesTestPage`!

---

## What You'll See on PlacesTestPage

### Page Layout

```
???????????????????????????????????????????
? Google Places API Test             [×] ?
???????????????????????????????????????????
? Session Token:                          ?
? abc123-defg-4567...                     ?
? [New Session]                           ?
???????????????????????????????????????????
? Location Bias:                          ?
? ? 26.1224, -80.1373                   ?
? Fort Lauderdale, FL                     ?
? [?? Refresh Location Bias]             ?
???????????????????????????????????????????
? Quota Status:                           ?
? Autocomplete: 0/1000 | Details: 0/500  ?
???????????????????????????????????????????
? Test Autocomplete                       ?
? ??????????????????????????????????????? ?
? ? Type an address...            [X]   ? ?
? ??????????????????????????????????????? ?
? [Search Now]                            ?
???????????????????????????????????????????
? Predictions:                            ?
? ??????????????????????????????????????? ?
? ? ?? 123 Main St, Fort Lauderdale...  ? ?
? ? ?? 123 Main St, Miami, FL...        ? ?
? ? ?? 123 Main Ave, Pompano Beach...   ? ?
? ??????????????????????????????????????? ?
???????????????????????????????????????????
? Selected Place Details                  ?
? Label: 123 Main Street                  ?
? Address: 123 Main St, Fort Laud...      ?
? Lat/Lng: 26.1234, -80.5678             ?
? Place ID: ChIJ...                       ?
???????????????????????????????????????????
? Test Log                                ?
? ??????????????????????????????????????? ?
? ? [14:23:15] New session started      ? ?
? ? [14:23:20] Searching for: '123'     ? ?
? ? [14:23:21] ? Found 5 predictions   ? ?
? ??????????????????????????????????????? ?
? [Clear Log]                             ?
???????????????????????????????????????????
```

---

## Testing Scenarios

### Scenario 1: Basic Autocomplete Test

**Steps:**
1. Type in search box: `123 Main St, Fort Lauderdale`
2. Wait 300ms (debounce)
3. Predictions appear automatically

**Expected:**
- ? 5 predictions shown
- ? Each shows full address
- ? Quota counter increments: `Autocomplete: 1/1000`
- ? Log shows: `[Time] ? Found 5 predictions in XXXms`

**Location Bias:**
- If GPS enabled ? Fort Lauderdale results prioritized
- If GPS denied ? US-wide results

---

### Scenario 2: Place Details Test

**Steps:**
1. Complete Scenario 1 (get predictions)
2. **Tap any prediction** in the list
3. Wait for Place Details to load

**Expected:**
- ? "Selected Place Details" section populates:
  - Label: Street address or place name
  - Address: Full formatted address
  - Lat/Lng: Coordinates (e.g., `26.1234, -80.5678`)
  - Place ID: Google's unique ID
- ? Quota counter increments: `Details: 1/500`
- ? Log shows: `[Time] ? Place details retrieved in XXXms`
- ? New session token generated

---

### Scenario 3: Location Bias Test

**Steps:**
1. Check "Location Bias" section on page load
2. Tap **"?? Refresh Location Bias"**

**Expected (GPS Enabled):**
- ? Shows: `? [Your Lat], [Your Lng]` (green)
- ? Shows your city/address
- ? Log shows: `Location bias: XX.XXXX, -YY.YYYY`

**Expected (GPS Denied):**
- ?? Shows: `?? Region-only (US)` (orange)
- ? Log shows: `Location bias: Region-only (no GPS)`

**Test:**
- Turn off GPS ? Refresh ? Should show orange warning
- Turn on GPS ? Refresh ? Should show green coordinates

---

### Scenario 4: Quota Tracking Test

**Steps:**
1. Check initial quota: `Autocomplete: 0/1000 | Details: 0/500`
2. Perform 5 searches
3. Select 2 places (get details)
4. Check quota again

**Expected:**
- ? Shows: `Autocomplete: 5/1000 | Details: 2/500`
- ? Counters persist across app restarts (uses Preferences)

**Quota Warning Test:**
- Make 800+ autocomplete requests (script or manual)
- Log should show: `[PlacesAPI] Warning: Approaching daily quota (80%)`

---

### Scenario 5: Error Handling Test

**Test A: No Internet**

**Steps:**
1. Turn off WiFi/mobile data
2. Type search query
3. Observe behavior

**Expected:**
- ? No crash
- ? Empty predictions returned
- ? Log shows: `[PlacesAPI] Autocomplete | NetworkError`
- ? User can keep typing

**Test B: Invalid Query**

**Steps:**
1. Type only 1-2 characters (e.g., "ab")
2. Observe behavior

**Expected:**
- ? No API call made
- ? Log shows: `Input too short (2 chars)`
- ? Min 3 characters enforced

---

### Scenario 6: Session Token Lifecycle

**Steps:**
1. Note current session token (e.g., `abc123...`)
2. Perform search ? Select place
3. Check session token again

**Expected:**
- ? Token changes after place selection
- ? Log shows: `New session started: xyz789...`
- ? All searches before selection used same token

**Why This Matters:**
- Session tokens group autocomplete requests for billing
- Google charges per **session**, not per keystroke
- Proper token management saves money ??

---

## Debugging Tips

### View Debug Output

**Visual Studio:**
1. Run app in Debug mode
2. Open **Output Window** (View ? Output)
3. Look for lines starting with `[PlacesAPI]` or `[PlacesAutocompleteService]`

**Example Output:**
```
[PlacesAutocompleteService] Initialized with dynamic location biasing
[PlacesAutocompleteService] Fetching user's current location for biasing...
[PlacesAutocompleteService] Location cached: 26.1224, -80.1373 (Fort Lauderdale, FL)
[PlacesAutocompleteService] Using location bias: 26.1224, -80.1373
[PlacesAutocompleteService] Autocomplete request: '123 Main' (session: abc123...)
[PlacesAPI] Autocomplete | Status: 200 OK | Latency: 342ms | Time: 14:23:15
[PlacesAutocompleteService] Autocomplete returned 5 predictions in 342ms
```

### Common Issues

| Issue | Likely Cause | Solution |
|-------|-------------|----------|
| No predictions appear | Query too short (< 3 chars) | Type at least 3 characters |
| "Region-only" warning | GPS permission denied | Enable location in device settings |
| Network error | No internet | Check WiFi/mobile data |
| Quota exceeded | 1000+ requests today | Wait until midnight UTC for reset |
| App crashes | Bug in code | Check Output window for exception |

---

## Performance Benchmarks

### Expected Latency (Good Network)

| Operation | Expected | Target |
|-----------|----------|--------|
| Autocomplete API | 200-500ms | < 500ms |
| Place Details API | 300-800ms | < 1000ms |
| GPS Location Fetch | 1-3 seconds (first time) | < 3s |
| GPS Location (cached) | 0ms | Instant |

### API Call Frequency

| Event | API Calls | Notes |
|-------|-----------|-------|
| User types "123" | 0 calls | Too short |
| User types "123 Main" | 1 call | After 300ms debounce |
| User types "123 Main St" | 1 call | Cancels previous, new call |
| User selects place | 1 call | Place Details |
| **Total per search** | **2-4 calls** | Autocomplete + Details |

---

## Next Steps After Testing

### ? Mark as Complete When:

- [ ] Autocomplete returns predictions
- [ ] Place Details returns coordinates
- [ ] Location bias shows your GPS location
- [ ] Quota tracking increments correctly
- [ ] Session tokens regenerate after selection
- [ ] Error handling works (offline, short query)

### ?? Then Proceed To:

**Phase 2:** Build `LocationAutocompleteView` component
- Reusable XAML component
- ViewModel with search logic
- Integration with saved locations
- Polish UX (loading states, error messages)

---

## Removing the Debug Button (Later)

When you're done with Phase 1 testing and ready for Phase 2:

1. **Remove the button** from `MainPage.xaml`:
   ```xml
   <!-- DELETE THIS -->
   <Button Text="?? Test Places API (Debug)" ... />
   ```

2. **Remove the handler** from `MainPage.xaml.cs`:
   ```csharp
   // DELETE THIS
   private async void OnPlacesTestClicked(object sender, EventArgs e) { ... }
   ```

3. **Keep the route** in `AppShell.xaml.cs` (still useful for debugging):
   ```csharp
   // KEEP THIS - might need it later
   Routing.RegisterRoute(nameof(Pages.PlacesTestPage), typeof(Pages.PlacesTestPage));
   ```

---

## Summary

?? **Quick Access:**
1. Run app ? Log in
2. Tap **"?? Test Places API (Debug)"** on MainPage
3. Start testing!

?? **What to Test:**
- Autocomplete (type query)
- Place Details (tap prediction)
- Location bias (GPS on/off)
- Quota tracking (check counters)
- Error handling (offline, short query)

? **Success Criteria:**
- Predictions appear
- Details load with coordinates
- No crashes
- Quota tracks correctly

**Ready to test Phase 1!** ??
