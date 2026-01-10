# Phase 0 Summary - Google Places Autocomplete Integration

**Version:** 1.1 (Revised after stakeholder feedback)  
**Date:** 24 December 2025  
**Status:** ✅ Ready for Phase 1  

---

## What Changed in This Revision

### Key Adjustments Made

1. **✅ Updated Pricing Model**
   - Removed outdated "$200/month credit" references
   - Updated to March 2025 SKU-based pricing with free monthly allowances
   - Clarified monitoring via Google Cloud Console usage dashboard

2. **✅ Realistic Security Posture**
   - Acknowledged API key is in mobile client (not server-hidden)
   - Protection via Google Cloud Console restrictions (package name, bundle ID, SHA-1, API scope)
   - Updated acceptance criteria to reflect mobile app reality

3. **✅ Persistent Quota Tracking**
   - Changed from in-memory to `Preferences`-based storage
   - Quota counters survive app restarts
   - Date-keyed storage with auto-reset at midnight UTC

4. **✅ Relaxed Type Filtering**
   - Removed strict `includedPrimaryTypes` filtering
   - Use location biasing (`regionCode: US`) instead
   - Prevents accidentally hiding valid addresses users need

5. **✅ Clarified iOS Configuration**
   - No extra setup for Places API (New) web service calls
   - Noted potential future SDK needs if embedding Google Maps

6. **✅ Split MVP vs. Hardening Criteria**
   - **MVP (73 criteria):** Must-pass to merge and feature-flag-enable
   - **Hardening (21 criteria):** Must-pass before default-on and GA
   - Allows shipping iteratively without blocking on "perfect"

---

## MVP Decisions (Resolved)

### 1. Regional Restrictions ✅
**Decision:** US-biased results for MVP, global later  
**How:** `locationBias` or `regionCode` in API requests  
**Why:** Most Bellwood rides are US-based; keeps results relevant without hard geo-fencing

### 2. Offline Caching ❌
**Decision:** Not MVP  
**Why:** Predictions are ephemeral; offline = saved locations + manual entry + GPS  
**Alternative:** Show saved/recent locations when offline

### 3. Business POI Prioritization ✅
**Decision:** Light prioritization, no strict filtering  
**How:** "Quick Picks" section for airports/hotels; avoid hard type filtering  
**Why:** Common for Bellwood; feels "made for them" without suppressing valid addresses

### 4. Recent/Favorite Locations ✅
**Decision:** Saved locations for MVP (simplified)  
**How:** Show saved locations above predictions when SearchBar focused/empty  
**Why:** Users repeat pickup/dropoff often; existing `ProfileService` data  
**Future:** Add "recent" tracking with ranking

---

## Scope Boundaries

### ✅ In Scope for MVP

- Real-time autocomplete with 300ms debounce
- Session token management (UUID v4, lifecycle management)
- Place Details lookup with coordinates
- US-biased results
- Saved locations integration in autocomplete UI
- Light POI prioritization (airports/hotels quick picks)
- Offline fallback to manual entry + GPS
- Error handling with graceful degradation
- Persistent quota tracking in `Preferences`
- Feature flag for safe rollout

### ❌ Out of Scope for MVP (Future)

- Offline caching of predictions
- Global/international location support
- "Recent searches" tracking/ranking
- Strict type filtering via `includedPrimaryTypes`
- Embedded map view in autocomplete
- Multi-language support

---

## Technical Highlights

### API Integration
- **Autocomplete:** POST to `https://places.googleapis.com/v1/places:autocomplete`
- **Place Details:** GET to `https://places.googleapis.com/v1/places/{placeId}`
- **Headers:** `X-Goog-Api-Key` for authentication, `X-Goog-FieldMask` for field selection
- **Biasing:** `locationBias` or `regionCode: "US"` for US-centric results

### Session Tokens
- UUID v4 generated when SearchBar focused
- Reused across all autocomplete calls in one search session
- Invalidated when:
  - User selects a place (Place Details called)
  - 3 minutes elapsed
  - SearchBar cleared
- Groups autocomplete calls together for billing (session-based pricing)

### Quota Management
- Persistent storage in `Preferences` with date key
- Structure: `{date: "yyyyMMdd", autocompleteCount: int, detailsCount: int, disabledUntil: DateTime?}`
- Auto-reset at midnight UTC
- Alert at 50% daily limit, disable at 90%
- Survives app restarts

### Debouncing
- 300ms delay after last keystroke
- Cancel previous in-flight requests
- Minimum 3 characters before querying
- Rate limit: Max 10 requests per 10 seconds

### Error Handling
- **Network errors:** Offline indicator, fallback to manual entry
- **Quota exceeded (429):** Toast message, disable for rest of day, manual entry
- **API errors (500):** Retry once, then fallback
- **No results:** "No suggestions found" message, allow continued typing

---

## Security Model

### API Key Protection
**Mobile Reality:** API key is in the client (HTTP header)  
**Protection Layers:**
1. **Google Cloud Console Restrictions:**
   - Android: Package name + SHA-1 fingerprints (debug + release)
   - iOS: Bundle ID
   - API scope: Places API (New) only
2. **Not in source control:** Use platform build secrets
3. **HTTPS enforced:** All API calls over TLS

**What this prevents:**
- ✅ Key reuse in other apps
- ✅ Key use for other Google APIs
- ✅ Unauthorized apps making requests
- ❌ Determined attacker reverse-engineering the app (accepted mobile risk)

---

## Acceptance Criteria Summary

### MVP Criteria (Required for Merge)
- Phase 1: Service Implementation (15 criteria)
- Phase 2: Component (16 criteria, including saved locations)
- Phase 3: QuotePage Integration (9 criteria)
- Phase 4: BookRidePage Integration (3 criteria)
- Phase 5: Error Handling (7 criteria)
- Phase 6: Basic Testing (5 criteria)
- Cross-Cutting Concerns (16 criteria)

**Total MVP:** 73 criteria

### Hardening Criteria (Required for GA)
- Performance Benchmarks (3 criteria)
- Load Testing (2 criteria)
- Accessibility Certification (2 criteria)
- Edge Cases (3 criteria)
- Advanced Testing (3 criteria)
- Deployment Readiness (8 criteria)

**Total Hardening:** 21 criteria

**Grand Total:** 94 criteria

---

## Component Structure

### New Files to Create

**Phase 1:**
- `Services/IPlacesAutocompleteService.cs` (interface)
- `Services/PlacesAutocompleteService.cs` (implementation)
- `Models/AutocompletePrediction.cs` (API response model)
- `Models/PlaceDetails.cs` (API response model)

**Phase 2:**
- `Components/LocationAutocompleteView.xaml` (UI)
- `Components/LocationAutocompleteView.xaml.cs` (code-behind)
- `ViewModels/LocationAutocompleteViewModel.cs` (optional, if MVVM)

**Phase 3-4:**
- Modifications to `QuotePage.xaml` and `.xaml.cs`
- Modifications to `BookRidePage.xaml` and `.xaml.cs`

**Documentation:**
- Update `LocationPickerService.md` to note Places autocomplete as primary
- Create troubleshooting guide for common errors
- Component usage examples in README

---

## Backwards Compatibility Guarantees

### What Stays Unchanged
- ✅ `ILocationPickerService` interface (existing)
- ✅ `LocationPickerService` implementation (existing map launching, geocoding)
- ✅ `Location` model structure (all existing fields)
- ✅ `ProfileService` CRUD operations
- ✅ Saved locations in pickers
- ✅ "Use Current Location" button (GPS + reverse geocoding)
- ✅ "Open Maps" for viewing (optional, post-selection)

### What Changes (Additive Only)
- ➕ New `IPlacesAutocompleteService` (separate from existing service)
- ➕ New `LocationAutocompleteView` component
- ➕ QuotePage/BookRidePage UI updated to include autocomplete
- ➕ Manual entry fields still visible (as fallback)

**Migration Path:**
- Phase 1-4: Autocomplete coexists with existing flows
- Feature flag controls autocomplete visibility
- Users can still use manual entry, GPS, saved locations
- No breaking changes to existing functionality

---

## Testing Strategy

### MVP Testing (Before Merge)
1. **Unit Tests:**
   - Service API call mocking
   - Session token lifecycle
   - Debouncing logic
   - Error response handling

2. **Integration Tests:**
   - End-to-end autocomplete flow
   - Fallback scenarios (offline, quota exceeded)
   - Saved locations integration

3. **Manual Testing:**
   - Android emulator + physical device
   - iOS simulator + physical device
   - Performance spot checks (latency <500ms)

### Hardening Testing (Before GA)
1. **Performance Benchmarks:**
   - 50+ requests over real network
   - Measure 95th percentile latency
   - UI responsiveness verification

2. **Load Testing:**
   - Quota limit stress tests
   - Rapid input/deletion
   - Network instability simulation

3. **Accessibility:**
   - Full TalkBack/VoiceOver testing
   - Keyboard navigation certification

---

## Rollout Plan

### Phase 1: Development & Testing (Feature Flag OFF)
- Implement Phases 1-6
- Unit and integration tests pass
- Code review completed
- Merge to main

### Phase 2: Internal Beta (Feature Flag ON for devs)
- Enable feature flag for development team only
- Dogfood autocomplete in daily use
- Monitor quota usage
- Collect feedback

### Phase 3: Limited Rollout (Feature Flag ON for 10% users)
- Gradual rollout to small user percentage
- Monitor error rates, latency, quota
- A/B test autocomplete vs. manual entry usage rates

### Phase 4: Full Rollout (Feature Flag ON for all)
- All MVP + Hardening criteria met
- Performance benchmarks confirmed
- Error rate <2% over 3 days
- Quota usage within safe limits
- Default-on for all users

---

## Risk Mitigation

### Risk: Quota Exceeded Mid-Day
**Mitigation:**
- Persistent tracking with 50% warning threshold
- Auto-disable at 90% with graceful fallback
- Daily monitoring alerts
- Feature flag for instant rollback

### Risk: API Latency Spike
**Mitigation:**
- 10-second timeout on all requests
- Retry logic (1 retry max)
- Fallback to manual entry
- Debouncing reduces request volume

### Risk: User Confusion with New UI
**Mitigation:**
- Saved locations shown first (familiar)
- Manual entry still visible
- Inline help text ("Search for an address...")
- Feature flag for rollback

### Risk: Geocoding Fails for Selected Place
**Mitigation:**
- Place Details includes coordinates
- No secondary geocoding needed
- If Place Details fails, user can retry or use manual entry

---

## Success Metrics (Post-MVP Launch)

### User Adoption
- **Target:** >70% of location entries use autocomplete (not manual entry)
- **Measure:** Track autocomplete selections vs. manual submissions

### Performance
- **Target:** Autocomplete latency <500ms average
- **Target:** Place Details latency <800ms average
- **Measure:** Log all request durations

### Cost
- **Target:** Stay within free monthly SKU allowances
- **Target:** <4 autocomplete requests per successful selection (session efficiency)
- **Measure:** Daily Cloud Console usage review

### Reliability
- **Target:** <2% error rate (network, API errors)
- **Measure:** Error logging and daily summaries

---

## Next Steps

### Immediate (Phase 0 Complete ✅)
1. ✅ Stakeholder approval of revised UX spec
2. ✅ Approval of revised acceptance criteria
3. ✅ Approval of MVP scope decisions
4. ✅ Sign-off on Phase 0

### Phase 1 Kickoff (Next)
1. ➡️ Create `IPlacesAutocompleteService` interface
2. ➡️ Implement `PlacesAutocompleteService` with session token management
3. ➡️ Create API response models (`AutocompletePrediction`, `PlaceDetails`)
4. ➡️ Register service in `MauiProgram.cs`
5. ➡️ Unit tests for service
6. ➡️ Validate PAC-1.1 through PAC-1.15

---

## Document Change Log

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 24 December 2025 | Initial Phase 0 deliverables | AI Assistant |
| 1.1 | 24 December 2025 | Revised per stakeholder feedback:<br>- Updated pricing model<br>- Realistic security posture<br>- Persistent quota tracking<br>- Relaxed type filtering<br>- MVP/Hardening split<br>- MVP decisions resolved | AI Assistant |

---

**Phase 0 Status:** ✅ **COMPLETE - READY FOR PHASE 1**

All stakeholder feedback incorporated. UX spec and acceptance criteria updated. MVP scope clearly defined. Technical approach validated. Security model realistic. Ready to begin implementation.
