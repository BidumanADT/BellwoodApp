# Google Places Autocomplete Integration - UX Specification

**Version:** 1.0  
**Date:** 24 December 2025  
**Status:** Phase 0 - Alignment & Guardrails  

---

## Overview

This specification defines the user experience for integrating Google Places Autocomplete into the Bellwood Elite mobile app for location selection (pickup and dropoff). The goal is to keep users in-app with real-time address suggestions while maintaining fallback options.

---

## Target User Experience

### Primary Flow: In-App Autocomplete

#### Happy Path

1. **User arrives at location entry** (QuotePage or BookRidePage)
   - Sees a `SearchBar` with placeholder: "Search for an address..."
   - Below it: Empty state or "Recent locations" if available

2. **User starts typing** (e.g., "123 Main")
   - After 300ms debounce, app queries Google Places Autocomplete (New) API
   - Dropdown appears below `SearchBar` showing up to 5 suggestions:
     ```
     📍 123 Main Street, Chicago, IL, USA
     📍 123 Main St, Evanston, IL, USA
     📍 123 Main Ave, Oak Park, IL, USA
     ```
   - Each suggestion shows icon + formatted address
   - Typing updates suggestions in real-time

3. **User selects a suggestion**
   - Tap on suggestion row
   - App calls Google Places Details API with `place_id`
   - Loading indicator appears briefly (spinner or subtle animation)
   - On success:
     - **Label field** auto-populated with business name OR street number + street (e.g., "123 Main Street")
     - **Address field** populated with full formatted address
     - **Coordinates** stored in `Location` object (Latitude, Longitude)
     - **PlaceId** stored for future reference
     - **IsVerified** set to `true`
   - SearchBar clears or shows selected address
   - User can optionally edit the label

4. **User saves the location**
   - Taps "Save Pickup Location" or "Save Dropoff Location"
   - Location added to ProfileService
   - Added to picker dropdown for future use

#### Alternative Path: View Selected Location in Maps

5. **User wants to verify location on map**
   - After selecting from autocomplete, a "View in Maps" button appears
   - Taps button → Opens native maps app with coordinates
   - User can view location, then returns to app
   - Location data already saved; no re-entry needed

---

### Fallback Flow: Manual Entry + Existing Flows

#### When Autocomplete is Unavailable

**Scenarios:**
- No internet connection
- Places API quota exceeded
- API returns error (500, 401, etc.)
- User in region with poor API coverage

**UX Response:**

1. **SearchBar shows error state**
   - Icon changes to ⚠️
   - Placeholder: "Offline - enter address manually"
   - Autocomplete dropdown hidden

2. **Manual entry fields appear**
   - Label: `Entry` field (e.g., "Home", "Office")
   - Address: `Entry` field (e.g., "123 Main St, Chicago, IL 60601")

3. **Optional: "Use Current Location" button**
   - Falls back to GPS + reverse geocoding via MAUI `IGeocoding`
   - Same behavior as current implementation

4. **Geocoding attempt on save**
   - When user taps "Save", app attempts to geocode the manual address
   - If successful: Coordinates populated, `IsVerified = true`
   - If fails: Coordinates remain null, `IsVerified = false` (still allows save)

---

## Error States & Handling

### Error Scenarios

| Error Type | Trigger | User Experience | Technical Response |
|------------|---------|-----------------|-------------------|
| **No Internet** | Device offline | SearchBar shows "Offline - enter address manually" | Switch to manual entry mode; disable autocomplete |
| **Quota Exceeded** | Daily/monthly limit hit | Toast: "Address search temporarily unavailable. Please enter manually." | Log to diagnostics; fallback to manual entry |
| **API Error (4xx/5xx)** | Invalid request or server error | SearchBar shows "Search unavailable - try again later" | Retry once after 2s; if fails, fallback to manual |
| **No Predictions** | User types gibberish or very specific query | Dropdown shows "No suggestions found - try a different address" | Allow user to continue typing or switch to manual |
| **Place Details Fail** | `place_id` lookup fails | Alert: "Unable to get location details. Try another address." | User returns to autocomplete; can select different suggestion |
| **Geocoding Fails** | Manual address can't be geocoded | Warning: "Location saved without coordinates. Add coordinates later?" | Save with `IsVerified = false`; allow editing |

### Error Message Guidelines

**Tone:** Helpful, not alarming  
**Action:** Always provide a path forward  
**Examples:**
- ✅ "Can't reach address search. Enter manually below."
- ❌ "ERROR: API FAILED"

---

## Visual Design Specs

### SearchBar (Autocomplete Entry)

```
┌─────────────────────────────────────────────────┐
│ 🔍  Search for an address...                   │
└─────────────────────────────────────────────────┘
```

**Styling:**
- Background: `BellwoodCharcoal` (#171B21)
- Text: `BellwoodCream` (#F5F7FA)
- Placeholder: `BellwoodPlaceholder` (#9AA3AF)
- Border: 1px `BellwoodGold` (#FFD166) when focused
- Height: 44pt (accessibility minimum)

### Suggestions Dropdown

```
┌─────────────────────────────────────────────────┐
│ 📍 123 Main Street, Chicago, IL 60601          │
├─────────────────────────────────────────────────┤
│ 📍 123 Main St, Evanston, IL 60201             │
├─────────────────────────────────────────────────┤
│ 📍 1230 Main Ave, Oak Park, IL 60302           │
└─────────────────────────────────────────────────┘
```

**Styling:**
- Each row: 44pt minimum height
- Background: `BellwoodCharcoal`
- Hover/Selected: Slightly lighter shade (#1F2329)
- Separator: 1px line (#2B2F36)
- Max visible rows: 5 (scroll if more)
- Icon: 📍 or use custom location pin

### Loading State

```
┌─────────────────────────────────────────────────┐
│ 🔍  123 Main...                        ⏳       │
└─────────────────────────────────────────────────┘
│ ⏳ Searching...                                 │
└─────────────────────────────────────────────────┘
```

### No Results State

```
┌─────────────────────────────────────────────────┐
│ 🔍  asdfghjkl                                   │
└─────────────────────────────────────────────────┘
│ No suggestions found - try a different address  │
│ [Enter address manually instead ↓]             │
└─────────────────────────────────────────────────┘
```

---

## Interaction States

### State Machine

```
[Idle: Empty SearchBar]
         ↓ User types
[Debouncing: Wait 300ms]
         ↓ Timer expires
[Fetching: API call in progress]
         ↓
    ┌────┴────┐
    ↓         ↓
[Success]  [Error]
 Show        Show error
 results     + fallback
    ↓
[User selects]
    ↓
[Fetching Details]
    ↓
[Location Populated]
```

### Debounce Strategy

- **Delay:** 300ms after last keystroke
- **Minimum characters:** 3 (don't query on "ab")
- **Cancel:** Previous in-flight request when new query starts
- **Purpose:** Reduce API calls and costs

---

## Google Places API Integration

### Endpoints to Use

1. **Autocomplete (New) API**
   - **Endpoint:** `https://places.googleapis.com/v1/places:autocomplete`
   - **Method:** POST
   - **Headers:** 
     - `Content-Type: application/json`
     - `X-Goog-Api-Key: [API_KEY]`
   - **Body:**
     ```json
     {
       "input": "123 Main",
       "sessionToken": "uuid-v4-here",
       "includedPrimaryTypes": ["street_address", "premise", "airport"],
       "languageCode": "en"
     }
     ```
   - **Response:** Array of predictions with `placeId`, `text.text`, `description`

2. **Place Details (New) API**
   - **Endpoint:** `https://places.googleapis.com/v1/places/{placeId}`
   - **Method:** GET
   - **Headers:**
     - `X-Goog-Api-Key: [API_KEY]`
     - `X-Goog-FieldMask: displayName,formattedAddress,location`
   - **Response:** 
     ```json
     {
       "displayName": { "text": "123 Main Street" },
       "formattedAddress": "123 Main Street, Chicago, IL 60601, USA",
       "location": { "latitude": 41.8781, "longitude": -87.6298 }
     }
     ```

### Session Token Strategy

**Purpose:** Group autocomplete requests together for billing (charged per session, not per keystroke)

**Implementation:**
1. Generate UUID when user focuses SearchBar
2. Include same token in all autocomplete requests during that search session
3. Invalidate token when:
   - User selects a place (Place Details called)
   - User clears SearchBar
   - 3 minutes elapsed (timeout)
4. Generate new token for next search

**Example:**
```csharp
private string _currentSessionToken = Guid.NewGuid().ToString();

void OnSearchBarFocused()
{
    _currentSessionToken = Guid.NewGuid().ToString();
}

void OnPlaceSelected()
{
    // Token consumed, reset for next search
    _currentSessionToken = Guid.NewGuid().ToString();
}
```

---

## Quota & Cost Management

### API Quotas (Google Cloud Console)

**Google Places API (New) - March 2025 Pricing Model:**

Places API (New) uses SKU-based pricing with monthly free usage allowances:

**Autocomplete (New):**
- SKU: Text-based (session-based billing)
- Free monthly usage: Check current allowances in Google Cloud Console
- Billed per session (all autocomplete calls in one session = 1 charge)
- **Our strategy:** Session tokens group requests; debounce to ~3-5 requests per session

**Place Details (New):**
- SKU: Basic Data (displayName, formattedAddress, location)
- Free monthly usage: Check current allowances in Google Cloud Console
- **Our strategy:** Only called once per selection

**Important:** Pricing and quotas are managed per-SKU in Google Cloud Console. Monitor your usage dashboard to stay within free tier limits.

### Quota Exceeded Strategy

**Daily/Monthly Limit Protection:**

1. Track request count persistently in `Preferences` (not just in-memory):
   ```csharp
   // Store quota tracking with date key
   var quotaKey = $"PlacesQuota_{DateTime.Today:yyyyMMdd}";
   var count = Preferences.Get(quotaKey, 0);
   Preferences.Set(quotaKey, count + 1);
   ```

2. If approaching 80% of daily safe limit:
   - Log warning to diagnostics
   - Show toast: "Address search nearing daily limit"

3. If quota exceeded:
   - Store disable flag in `Preferences` with expiry timestamp
   - Disable autocomplete until next day (midnight UTC)
   - Show: "Search unavailable - enter manually"
   - Fall back to manual entry + MAUI geocoding

4. Auto-reset at midnight UTC:
   - Check stored date vs. current date
   - Clear counters and flags when date changes

**Monitoring:**
- Log all API calls with SKU type to diagnostics
- Daily summary in diagnostics showing requests per SKU
- Alert if daily usage >50% by noon
- Weekly review of Cloud Console usage dashboard

---

## Accessibility

### Requirements

✅ **Screen Reader Support:**
- SearchBar announced as "Search for address, edit text"
- Suggestions announced as "Suggestion 1 of 5: 123 Main Street, Chicago"
- Selection announced: "Selected: 123 Main Street"

✅ **Keyboard Navigation:**
- Tab to SearchBar
- Arrow keys to navigate suggestions
- Enter to select
- Escape to cancel

✅ **Touch Targets:**
- Minimum 44x44 pt for all interactive elements
- Suggestion rows: 44pt height minimum

✅ **Color Contrast:**
- Text on background: WCAG AA compliant (4.5:1)
- Error states: Use icon + color

---

## Platform-Specific Considerations

### Android
- Google Maps API Key already configured in `AndroidManifest.xml`
- Use same key for Places API (New)
- Configure key restrictions in Google Cloud Console:
  - Package name: `com.bellwoodglobal.mobile`
  - SHA-1 fingerprints for debug and release keystores
- Handle back button during autocomplete (close dropdown)

### iOS
- No additional configuration needed for Places API (New) web service calls
- Places API (New) is a REST API, not an SDK - no maps framework required
- **Note:** If future phases embed Google Maps SDK for iOS, additional setup required
- Test keyboard behavior (autocorrect, suggestions)
- Handle safe area insets for dropdown

### Windows
- Test keyboard shortcuts (Ctrl+A, Ctrl+C)
- Mouse hover states for suggestions
- Bing Maps integration remains available via existing `LocationPickerService`

---

## Migration Path

### Coexistence with Existing Flows

**Phase 1-3 (During rollout):**
- Autocomplete available for new locations
- Saved locations still in picker dropdown
- "Use Current Location" button remains
- "Open Maps" available as secondary option

**Phase 4+ (Eventual state):**
- Autocomplete primary method
- Manual entry fallback
- Current location fallback
- Maps viewing optional

### Backward Compatibility

**Preserve:**
- `ILocationPickerService` interface (keep for map launching)
- `Location` model structure (all existing fields)
- ProfileService CRUD operations
- Saved locations in pickers

**New:**
- `IPlacesAutocompleteService` (separate service)
- `LocationAutocompleteView` (reusable component)
- Session token management

---

## Success Metrics (Post-Launch)

### User Experience
- **Primary:** % of locations selected via autocomplete vs. manual
- **Target:** >80% use autocomplete when available

### Performance
- **API Latency:** <500ms for autocomplete, <1s for details
- **Debounce Effectiveness:** <5 API calls per successful selection
- **Error Rate:** <2% failed geocoding attempts

### Cost
- **Monthly API Cost:** <$50/month (under free tier)
- **Average Requests per Search:** <4 autocomplete + 1 details

---

## Open Questions (To Resolve in Phase 0)

### ✅ RESOLVED - MVP Decisions

#### 1. **Regional restrictions on API key?**
**Decision:** ✅ US-biased results for MVP, global support later
- **Implementation:** Use `locationBias` or `regionCode` in API requests to bias results to US
- **Rationale:** Most Bellwood rides are US-based; keeps results relevant without hard geo-fencing the key
- **Future:** Global expansion ready when needed (just adjust bias parameter)

#### 2. **Offline caching?**
**Decision:** ❌ Not MVP
- **Rationale:** Predictions are ephemeral suggestions; caching adds complexity and staleness risk
- **Alternative (MVP):** Show saved locations and recent searches when offline instead
- **Offline strategy:** Existing manual entry + saved locations + "Use current location" via MAUI GPS

#### 3. **Business POI prioritization?**
**Decision:** ✅ Light prioritization for MVP (no strict filtering)
- **Approach:** 
  - Show "Quick Picks" section above results when appropriate (Airports, Hotels)
  - Use location biasing to elevate airports/hotels when input matches
  - **Avoid** hard filtering via `includedPrimaryTypes` to prevent suppressing valid addresses
- **Rationale:** Airport/hotel runs are common for Bellwood; this makes the app feel "made for them"

#### 4. **Recent/favorite locations?**
**Decision:** ✅ Saved locations for MVP (simplified version)
- **MVP Implementation:**
  - Show "Saved Locations" section above predictions when SearchBar focused and empty
  - Once user types, show predictions normally (saved locations can stay as small header)
  - Use existing `ProfileService` saved locations (already implemented)
- **Future:** Add "Recent" tracking with ranking/frequency sorting

---

## MVP Scope Summary

**In Scope for MVP:**
- ✅ Real-time autocomplete with debouncing
- ✅ Session token management for cost control
- ✅ Place Details lookup with coordinates
- ✅ US-biased results
- ✅ Saved locations shown in autocomplete UI
- ✅ Light POI prioritization (airports/hotels)
- ✅ Offline fallback to manual entry
- ✅ Error handling with graceful degradation
- ✅ Persistent quota tracking

**Out of Scope for MVP (Future Phases):**
- ❌ Offline caching of predictions
- ❌ Global/international location support
- ❌ "Recent searches" tracking
- ❌ Strict type filtering
- ❌ Embedded map view in autocomplete
- ❌ Multi-language support

---

## Approval Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Product Owner | | | |
| Tech Lead | | | |
| UX Designer | | | |
| QA Lead | | | |

---

**Next Steps:** Proceed to Phase 1 implementation after all stakeholders approve.
