# Testing Guide - Google Places Autocomplete

**Feature:** Address Autocomplete  
**Last Updated:** January 10, 2026  
**Status:** ? Complete

---

## ?? Test Objectives

Verify that Google Places autocomplete works correctly in all scenarios:
- ? Real-time predictions
- ? Place details with coordinates
- ? Saved locations integration
- ? Error handling
- ? Quota management

---

## ?? Test Scenarios

### Test 1: Basic Autocomplete ?

**Preconditions:** None

**Steps:**
1. Navigate to Quote Page or Book Ride Page
2. Tap "New Location" for pickup
3. Focus on search bar
4. Type "123 Main St"
5. Wait 300ms (automatic debouncing)
6. Observe predictions appear
7. Tap first prediction
8. Observe label and address auto-fill

**Expected Results:**
- ? Predictions appear within 500ms
- ? At least 3-5 predictions shown
- ? Tapping prediction auto-fills label + address
- ? Coordinates captured (lat/lng not null)
- ? Manual entry fields populated

**Pass Criteria:** All expected results met

---

### Test 2: Saved Locations First ?

**Preconditions:** User has saved locations

**Steps:**
1. Tap "New Location"
2. Focus on search bar (don't type anything)
3. Observe dropdown

**Expected Results:**
- ? Saved locations appear immediately
- ? No API call made (instant display)
- ? Saved locations show custom labels
- ? Tapping saved location selects it

**Pass Criteria:** Saved locations work without API call

---

### Test 3: Debouncing Works ?

**Preconditions:** None

**Steps:**
1. Type quickly: "O'Hare Airport" (in < 300ms)
2. Observe API calls in logs
3. Count number of API requests

**Expected Results:**
- ? Only 1-2 API calls made (not one per keystroke)
- ? Final prediction matches full query
- ? No "Too Many Requests" errors

**Pass Criteria:** < 5 API calls for full query

---

### Test 4: Special Characters ?

**Preconditions:** None

**Steps:**
1. Type: "123 Main St, Apt #5"
2. Type: "O'Hare"
3. Type: "New York, NY"
4. Type: "San José"

**Expected Results:**
- ? No "Malformed URL" errors
- ? Predictions work correctly
- ? Special characters preserved in results

**Pass Criteria:** All special characters handled

---

### Test 5: Error Handling - Offline ?

**Preconditions:** None

**Steps:**
1. Turn off WiFi and mobile data
2. Type "123 Main St"
3. Wait 5 seconds
4. Observe error message

**Expected Results:**
- ? "Network error" message shown
- ? Manual entry still available
- ? Saved locations still shown
- ? No app crash

**Pass Criteria:** Graceful degradation to manual entry

---

### Test 6: Error Handling - API Error ?

**Preconditions:** Simulate API error (backend team)

**Steps:**
1. Backend returns 500 error
2. Type "123 Main St"
3. Observe behavior

**Expected Results:**
- ? Error message shown
- ? Manual entry available
- ? Retry on next search

**Pass Criteria:** No crash, graceful fallback

---

### Test 7: Quota Management - 50% Alert ?

**Preconditions:** Set quota to 50 requests, use 25

**Steps:**
1. Make 25th autocomplete search
2. Observe alert

**Expected Results:**
- ? Alert shown: "50% of daily quota used"
- ? Autocomplete still works
- ? Alert dismissable

**Pass Criteria:** Alert shown, autocomplete continues

---

### Test 8: Quota Management - 90% Disable ?

**Preconditions:** Set quota to 100 requests, use 90

**Steps:**
1. Make 90th autocomplete search
2. Try to make 91st search

**Expected Results:**
- ? Alert shown: "Daily quota exceeded"
- ? Autocomplete disabled
- ? Manual entry available
- ? Saved locations still work

**Pass Criteria:** Autocomplete disabled, manual entry works

---

### Test 9: Form State Persistence ?

**Preconditions:** None

**Steps:**
1. Select location via autocomplete
2. Verify coordinates populated
3. Fill out rest of form
4. Close app (force quit)
5. Reopen app
6. Navigate to saved draft

**Expected Results:**
- ? Label and address restored
- ? Coordinates restored (lat/lng)
- ? "View in Maps" button works

**Pass Criteria:** Coordinates persist across app restarts

---

### Test 10: QuotePage Integration ?

**Preconditions:** None

**Steps:**
1. Navigate to Quote Page
2. Test pickup autocomplete
3. Test dropoff autocomplete
4. Submit quote

**Expected Results:**
- ? Both pickup and dropoff work
- ? Coordinates sent to backend
- ? Quote submitted successfully

**Pass Criteria:** End-to-end quote flow works

---

### Test 11: BookRidePage Integration ?

**Preconditions:** None

**Steps:**
1. Navigate to Book Ride Page
2. Test pickup autocomplete
3. Test dropoff autocomplete
4. Submit booking

**Expected Results:**
- ? Both pickup and dropoff work
- ? Coordinates sent to backend
- ? Booking created successfully

**Pass Criteria:** End-to-end booking flow works

---

## ?? Test Results Matrix

| Test # | Scenario | Android | iOS | Windows | Status |
|--------|----------|---------|-----|---------|--------|
| 1 | Basic Autocomplete | ? | ? | ? | Pass |
| 2 | Saved Locations | ? | ? | ? | Pass |
| 3 | Debouncing | ? | ? | ? | Pass |
| 4 | Special Characters | ? | ? | ? | Pass |
| 5 | Offline | ? | ? | ? | Pass |
| 6 | API Error | ? | ? | ? | Pass |
| 7 | Quota 50% | ? | ? | ? | Pass |
| 8 | Quota 90% | ? | ? | ? | Pass |
| 9 | Form Persistence | ? | ? | ? | Pass |
| 10 | QuotePage | ? | ? | ? | Pass |
| 11 | BookRidePage | ? | ? | ? | Pass |

**Legend:**
- ? Tested & Passed
- ? Not Yet Tested
- ? Failed
- ?? Pass with Issues

---

## ?? Known Issues

*None currently*

---

## ?? Regression Testing Checklist

Before each release, verify:
- [ ] Basic autocomplete works
- [ ] Offline mode gracefully degrades
- [ ] Quota management works
- [ ] Form persistence includes coordinates
- [ ] Both QuotePage and BookRidePage work

---

## ?? Related Documents

- `Feature-GooglePlacesAutocomplete.md` - Full implementation details
- `Reference-BugFixes.md` - Bug fix history
- `HowTo-SetupGoogleCloud.md` - Cloud console setup

---

**Status:** ? All scenarios pass on Android  
**Version:** 1.0  
**Maintainer:** QA Team
