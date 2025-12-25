# Google Places Autocomplete - Acceptance Criteria Checklist

**Version:** 1.0  
**Date:** 24 December 2025
**Phase:** 0 - Alignment & Guardrails  

---

## Purpose

This document defines the acceptance criteria for Google Places Autocomplete integration into the Bellwood Elite mobile app. Each criterion must be met before the feature is considered complete and ready for production.

**Criteria are split into two tiers:**
- **MVP Criteria:** Must-pass to merge to main and enable feature flag
- **Hardening Criteria:** Must-pass before default-on and general availability

---

## MVP Acceptance Criteria
*These must be met before merging and feature-flag enabling*

### Phase 1: PlacesAutocompleteService Implementation (MVP)

#### Service Architecture

- [ ] **PAC-1.1:** `IPlacesAutocompleteService` interface created with methods:
  - `Task<AutocompletePrediction[]> GetPredictionsAsync(string input, string sessionToken, CancellationToken ct = default)`
  - `Task<PlaceDetails?> GetPlaceDetailsAsync(string placeId, CancellationToken ct = default)`
  
- [ ] **PAC-1.2:** `PlacesAutocompleteService` implements `IPlacesAutocompleteService`

- [ ] **PAC-1.3:** Service registered in `MauiProgram.cs` as singleton

- [ ] **PAC-1.4:** HttpClient configured with:
  - Base URL: `https://places.googleapis.com/v1/`
  - API key passed in `X-Goog-Api-Key` header
  - Timeout: 10 seconds
  - Retry policy: 1 retry on network failure

#### API Integration

- [ ] **PAC-1.5:** Autocomplete API request includes:
  - `input` (user's search query)
  - `sessionToken` (UUID format)
  - `locationBias` or `regionCode` set to US (bias results to United States)
  - `languageCode` ("en")
  - **Note:** `includedPrimaryTypes` initially omitted to avoid filtering valid results; can add light biasing later if needed

- [ ] **PAC-1.6:** Place Details API request includes:
  - `placeId` in URL path
  - `X-Goog-FieldMask` header with: `displayName,formattedAddress,location`

- [ ] **PAC-1.7:** API responses deserialized correctly to C# models:
  - `AutocompletePrediction` (PlaceId, Description, MainText, SecondaryText)
  - `PlaceDetails` (DisplayName, FormattedAddress, Latitude, Longitude)

- [ ] **PAC-1.8:** Error responses handled gracefully:
  - 400/401 → Log error, return empty results
  - 429 (quota exceeded) → Log warning, return empty results
  - 500/503 → Retry once, then return empty results
  - Network timeout → Return empty results

#### Session Token Management

- [ ] **PAC-1.9:** Session token generated as UUID v4

- [ ] **PAC-1.10:** Session token reused across autocomplete requests in same search session

- [ ] **PAC-1.11:** Session token invalidated when:
  - User selects a place (Place Details called)
  - 3 minutes elapsed since creation
  - User clears search input

- [ ] **PAC-1.12:** New session token generated after invalidation

#### Debouncing & Throttling

- [ ] **PAC-1.13:** Debounce mechanism implemented:
  - 300ms delay after last keystroke before API call
  - Previous in-flight requests cancelled when new query starts

- [ ] **PAC-1.14:** Minimum query length enforced (3 characters)

- [ ] **PAC-1.15:** Request rate limited to max 10 requests per 10 seconds

---

### Phase 2: LocationAutocompleteView Component (MVP)

#### Component Structure

- [ ] **PAC-2.1:** `LocationAutocompleteView.xaml` created in `Components/` folder

- [ ] **PAC-2.2:** `LocationAutocompleteView.xaml.cs` code-behind created

- [ ] **PAC-2.3:** ViewModel created with properties:
  - `SearchText` (bindable string)
  - `Predictions` (ObservableCollection of predictions)
  - `IsLoading` (bool)
  - `ErrorMessage` (string)
  - `SelectedLocation` (Location)

#### UI Elements

- [ ] **PAC-2.4:** SearchBar with:
  - Placeholder: "Search for an address..."
  - Magnifying glass icon
  - Clear button
  - Styling matches `BellwoodCharcoal` theme

- [ ] **PAC-2.5:** Suggestions ListView with:
  - Item template showing icon + address text
  - 44pt minimum row height
  - Max 5 visible rows (scrollable)
  - Separator lines between rows

- [ ] **PAC-2.6:** Loading indicator:
  - Spinner shown when `IsLoading = true`
  - Positioned at end of SearchBar

- [ ] **PAC-2.7:** Error message label:
  - Shown when `ErrorMessage` not null
  - Styled with warning icon + text
  - Dismissible

- [ ] **PAC-2.8:** "No results" state:
  - Shown when predictions empty after successful API call
  - Message: "No suggestions found - try a different address"

#### Behavior

- [ ] **PAC-2.9:** Typing in SearchBar triggers:
  - Debounced API call via `IPlacesAutocompleteService`
  - `IsLoading = true` during request
  - `Predictions` updated on success
  - `ErrorMessage` set on failure

- [ ] **PAC-2.10:** Selecting a prediction:
  - Calls `GetPlaceDetailsAsync` with selected `placeId`
  - Shows loading indicator
  - On success:
    - Creates `Location` object with coordinates
    - Raises `LocationSelected` event
    - Clears SearchBar or shows selected address
  - On failure:
    - Shows error alert
    - User can try another selection

- [ ] **PAC-2.11:** SearchBar cleared:
  - Predictions list hidden
  - Session token invalidated
  - Error message cleared

#### Reusability

- [ ] **PAC-2.12:** Component exposes public events:
  - `LocationSelected(Location location)`
  - `SearchCleared()`

- [ ] **PAC-2.13:** Component accepts parameters:
  - `Placeholder` (string)
  - `InitialLocation` (Location, optional)

#### Saved Locations Integration (MVP Feature)

- [ ] **PAC-2.14:** Saved locations section shown when SearchBar focused and empty:
  - Queries `IProfileService.GetSavedLocations()`
  - Displays up to 5 most recent/favorite locations
  - Each location shows icon + label + truncated address
  - Tapping a saved location immediately populates fields (no API call needed)

- [ ] **PAC-2.15:** Saved locations remain visible (optional) when user starts typing:
  - Small header row above API predictions
  - Or hidden once predictions load (design decision)

- [ ] **PAC-2.16:** Saved location selection:
  - Bypasses API entirely
  - Raises `LocationSelected` event with existing `Location` object
  - Coordinates already present (if previously geocoded)

---

### Phase 3: QuotePage Integration (MVP)

#### UI Changes

- [ ] **PAC-3.1:** "New Pickup" section updated:
  - `LocationAutocompleteView` added above manual entry fields
  - "Pick from Maps" button replaced/supplemented with autocomplete

- [ ] **PAC-3.2:** "New Dropoff" section updated:
  - Same as pickup section

- [ ] **PAC-3.3:** Manual entry fields:
  - Still visible as fallback
  - Auto-populated when autocomplete selection made

#### Behavior

- [ ] **PAC-3.4:** Pickup autocomplete selection:
  - Populates `PickupNewLabel.Text` with place name or street
  - Populates `PickupNewAddress.Text` with formatted address
  - Stores coordinates in temporary `Location` object
  - Sets `IsVerified = true`

- [ ] **PAC-3.5:** Dropoff autocomplete selection:
  - Same behavior as pickup

- [ ] **PAC-3.6:** "Save Pickup/Dropoff" button:
  - Uses autocomplete-populated data if available
  - Falls back to manual entry if autocomplete not used
  - Validation still checks label + address not empty

- [ ] **PAC-3.7:** Error handling:
  - If autocomplete unavailable, show manual entry fields
  - Display user-friendly error message
  - Log error to diagnostics

#### Fallback Flows

- [ ] **PAC-3.8:** "Use Current Location" button still works:
  - Uses existing GPS + reverse geocoding flow
  - Bypasses autocomplete

- [ ] **PAC-3.9:** Saved locations picker still works:
  - No changes to existing behavior
  - Can still select from saved locations

---

### Phase 4: BookRidePage Integration (MVP)

- [ ] **PAC-4.1:** Same acceptance criteria as Phase 3 (QuotePage)

- [ ] **PAC-4.2:** All three pages (QuotePage, BookRidePage) use identical component

- [ ] **PAC-4.3:** No code duplication between pages

---

### Phase 5: Error Handling & Fallbacks (MVP)

#### Network Errors

- [ ] **PAC-5.1:** No internet connection:
  - Autocomplete shows "Offline - enter address manually"
  - Manual entry fields auto-shown
  - No error alerts (just inline message)

- [ ] **PAC-5.2:** API timeout (>10s):
  - Same as no internet
  - Logged to diagnostics

#### API Errors

- [ ] **PAC-5.3:** Quota exceeded (429):
  - Toast message: "Address search temporarily unavailable"
  - Autocomplete disabled for 1 hour
  - Manual entry shown
  - Logged with high priority

- [ ] **PAC-5.4:** Invalid API key (401):
  - Logged as critical error
  - Fallback to manual entry
  - Alert shown to user (dev mode only)

- [ ] **PAC-5.5:** Server error (500):
  - Retry once after 2 seconds
  - If still fails, fallback to manual entry
  - Logged as warning

#### Data Errors

- [ ] **PAC-5.6:** No predictions returned:
  - Show "No suggestions found" message
  - Allow user to continue typing or use manual entry

- [ ] **PAC-5.7:** Place Details fail for selected prediction:
  - Alert: "Unable to get location details. Try another address."
  - User can select different prediction
  - Original search results still visible

---

### Phase 6: Testing & Quality Assurance (MVP)

#### Unit Tests

- [ ] **PAC-6.1:** `PlacesAutocompleteService` tests:
  - Mock HTTP responses for success/failure cases
  - Verify request headers and body
  - Test debouncing logic
  - Test session token generation/invalidation

- [ ] **PAC-6.2:** `LocationAutocompleteView` ViewModel tests:
  - Test search triggering API calls
  - Test prediction selection
  - Test error state handling

#### Integration Tests

- [ ] **PAC-6.3:** QuotePage autocomplete flow:
  - Type address → Select prediction → Verify fields populated
  - Test on Android emulator
  - Test on iOS simulator
  - Test on Windows

- [ ] **PAC-6.4:** BookRidePage autocomplete flow:
  - Same as QuotePage

- [ ] **PAC-6.5:** Fallback scenarios:
  - Disable internet → Verify manual entry works
  - Invalid API key → Verify graceful degradation

#### Manual Testing Checklist

- [ ] **PAC-6.6:** Android physical device testing:
  - Autocomplete works
  - Keyboard behavior correct
  - Performance acceptable (<500ms latency)

- [ ] **PAC-6.7:** iOS physical device testing:
  - Same as Android

- [ ] **PAC-6.8:** Accessibility testing:
  - Screen reader announces autocomplete results
  - Keyboard navigation works
  - Touch targets >44pt

---

### Phase 7: Performance & Optimization (MVP)

#### Performance Metrics

- [ ] **PAC-7.1:** Autocomplete API latency:
  - Average <500ms measured over 50 requests
  - 95th percentile <1000ms

- [ ] **PAC-7.2:** Place Details API latency:
  - Average <800ms
  - 95th percentile <1500ms

- [ ] **PAC-7.3:** UI responsiveness:
  - SearchBar input lag <50ms
  - Dropdown appears <100ms after API response

#### API Quota Management

- [ ] **PAC-7.4:** Request tracking implemented with persistence:
  - Count requests per day in `Preferences` with date-keyed storage
  - Store: `{date: "yyyyMMdd", autocompleteCount: int, detailsCount: int, disabledUntil: DateTime?}`
  - Alert when >50% daily safe limit used
  - Disable autocomplete when >90% used, store disable flag with expiry
  - Auto-reset counters at midnight UTC (check date key on each request)

- [ ] **PAC-7.5:** Debounce effectiveness:
  - Average <5 autocomplete requests per selection
  - 90% of search sessions use ≤3 requests before selection
  - Session token properly groups requests for billing

#### Memory & Battery

- [ ] **PAC-7.6:** No memory leaks:
  - Component disposed correctly
  - Event handlers unsubscribed
  - HttpClient requests cancelled on navigation

- [ ] **PAC-7.7:** Battery impact minimal:
  - Background API calls cancelled
  - Debouncing prevents excessive requests

---

## Hardening Acceptance Criteria
*These must be met before default-on and GA*

### Phase 6+: Advanced Testing & Quality Assurance

#### Performance Benchmarking

- [ ] **PAC-HARD.1:** Autocomplete API latency measured:
  - Average <500ms over 50 production-like requests
  - 95th percentile <1000ms
  - Tested on real devices (not just emulators)

- [ ] **PAC-HARD.2:** Place Details API latency measured:
  - Average <800ms
  - 95th percentile <1500ms

- [ ] **PAC-HARD.3:** UI responsiveness verified:
  - SearchBar input lag <50ms
  - Dropdown appears <100ms after API response
  - No janky scrolling or UI freezes

#### Load Testing

- [ ] **PAC-HARD.4:** Quota tracking stress test:
  - Simulate hitting 80% daily quota
  - Verify warnings appear correctly
  - Simulate exceeding quota
  - Verify autocomplete disables and re-enables at midnight UTC

- [ ] **PAC-HARD.5:** Session token lifecycle tested:
  - Multiple rapid sessions created/invalidated correctly
  - No token leaks or reuse across sessions
  - 3-minute expiry enforced

#### Accessibility Certification

- [ ] **PAC-HARD.6:** Full screen reader testing:
  - TalkBack (Android) announces all states correctly
  - VoiceOver (iOS) announces all states correctly
  - Screen reader can select predictions without visual cues

- [ ] **PAC-HARD.7:** Keyboard navigation comprehensive:
  - Tab order logical
  - Arrow keys navigate predictions
  - Enter/Space select
  - Escape cancels

#### Edge Cases

- [ ] **PAC-HARD.8:** Network instability handling:
  - Test with intermittent connection (airplane mode toggle)
  - Test with slow 2G simulation
  - Verify no crashes or frozen UI

- [ ] **PAC-HARD.9:** Rapid input stress test:
  - Type very fast, delete, retype
  - Verify no duplicate API calls
  - Verify no stale results displayed

- [ ] **PAC-HARD.10:** Multi-language device testing:
  - Test on device with non-English locale
  - Verify results still in English (as configured)
  - Verify no encoding issues

---

## Cross-Cutting Concerns

### Security

- [ ] **PAC-SEC.1:** API key stored securely:
  - Not checked into source control
  - Stored in platform-specific secure configuration (appsettings, build secrets)
  - Restricted by app identity (Android package name + SHA-1, iOS bundle ID)
  - Restricted to Places API (New) only in Google Cloud Console

- [ ] **PAC-SEC.2:** API key restrictions configured in Google Cloud Console:
  - **Android:** Restricted to app package name (`com.bellwoodglobal.mobile`) + debug/release SHA-1 fingerprints
  - **iOS:** Restricted to app bundle ID
  - API restrictions: Places API (New) only
  - No IP restrictions (not applicable for mobile clients)

- [ ] **PAC-SEC.3:** HTTPS enforced for all API calls

**Note:** Mobile apps must include the API key in HTTP requests (via header). Security is enforced through Google Cloud Console restrictions (app identity + API scope), not by hiding the key from the client.

### Logging & Diagnostics

- [ ] **PAC-LOG.1:** All API requests logged with:
  - Timestamp
  - Endpoint
  - Response code
  - Latency

- [ ] **PAC-LOG.2:** Errors logged with stack traces (debug mode)

- [ ] **PAC-LOG.3:** Quota tracking logged daily

### Backwards Compatibility

- [ ] **PAC-COMPAT.1:** Existing `ILocationPickerService` unchanged

- [ ] **PAC-COMPAT.2:** Saved locations in ProfileService still work

- [ ] **PAC-COMPAT.3:** Manual entry still available

- [ ] **PAC-COMPAT.4:** "Use Current Location" still works

- [ ] **PAC-COMPAT.5:** "Open Maps" still works (optional viewing)

---

## Documentation

- [ ] **PAC-DOC.1:** UX spec approved by stakeholders

- [ ] **PAC-DOC.2:** API integration documented in code comments

- [ ] **PAC-DOC.3:** Component usage examples in README

- [ ] **PAC-DOC.4:** Troubleshooting guide for common errors

- [ ] **PAC-DOC.5:** Update existing LocationPickerService docs

---

## Deployment Readiness

### Pre-Production

- [ ] **PAC-DEPLOY.1:** All acceptance criteria above met

- [ ] **PAC-DEPLOY.2:** Code reviewed by at least 2 developers

- [ ] **PAC-DEPLOY.3:** QA testing completed (all test cases passed)

- [ ] **PAC-DEPLOY.4:** Performance benchmarks met

- [ ] **PAC-DEPLOY.5:** API quota monitoring configured

### Production

- [ ] **PAC-DEPLOY.6:** Feature flag ready (can disable if issues arise)

- [ ] **PAC-DEPLOY.7:** Rollback plan documented

- [ ] **PAC-DEPLOY.8:** Monitoring alerts configured for:
  - API error rate >5%
  - Quota usage >80%
  - Latency >2s

---

## Sign-off

### MVP Criteria (Required for Merge & Feature Flag)

| Phase | Criteria Met | Tester | Date | Notes |
|-------|--------------|--------|------|-------|
| Phase 1: Service | __ / 15 | | | |
| Phase 2: Component | __ / 16 | | | |
| Phase 3: QuotePage | __ / 9 | | | |
| Phase 4: BookRidePage | __ / 3 | | | |
| Phase 5: Error Handling | __ / 7 | | | |
| Phase 6: Basic Testing | __ / 5 | | | |
| Cross-Cutting | __ / 5 | | | |
| Security | __ / 3 | | | |
| Logging | __ / 3 | | | |
| Compatibility | __ / 5 | | | |
| Documentation | __ / 5 | | | |

**MVP Total:** __ / 76 criteria met

### Hardening Criteria (Required for GA/Default-On)

| Category | Criteria Met | Tester | Date | Notes |
|----------|--------------|--------|------|-------|
| Performance Benchmarks | __ / 3 | | | |
| Load Testing | __ / 2 | | | |
| Accessibility Certification | __ / 2 | | | |
| Edge Cases | __ / 3 | | | |
| Advanced Testing | __ / 3 | | | |
| Deployment Readiness | __ / 8 | | | |

**Hardening Total:** __ / 21 criteria met

**Grand Total:** __ / 97 criteria met

---

## Definition of Done

### MVP Done (Feature Flag On)
The Google Places Autocomplete integration is considered **MVP DONE** when:

1. ✅ All 76 MVP acceptance criteria are met
2. ✅ Phases 1-6 (MVP portions) signed off by QA
3. ✅ Code merged to main branch with feature flag OFF by default
4. ✅ MVP documentation complete
5. ✅ Feature flag tested (on/off behavior verified)

### GA Done (Default-On Ready)
The feature is considered **READY FOR GA** when:

1. ✅ All 21 Hardening criteria are met
2. ✅ All 97 criteria (MVP + Hardening) validated
3. ✅ Performance benchmarks confirmed on real devices
4. ✅ Production deployment successful with feature flag ON
5. ✅ Monitoring confirms <2% error rate over 3 days
6. ✅ Quota usage within safe limits over 1 week

---
