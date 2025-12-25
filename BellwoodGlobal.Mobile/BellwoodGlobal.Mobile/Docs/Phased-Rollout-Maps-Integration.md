## Phase 0 — Alignment and guardrails (½–1 day)
Completed 24 December 2025

**Goal:** Everyone agrees on UX + technical approach before touching code.

**Work**

* Confirm target UX:

  * Autocomplete for **Pickup** and **Dropoff**
  * Selecting a suggestion populates **Label + Address + Lat/Lng**
  * Optional “View in Maps” stays available, but is no longer required for picking
* Confirm Places endpoints to use: **Autocomplete + Place Details**
* Confirm quota strategy + error UX (what happens when Places fails/quota exceeded)
* Confirm API key storage + restrictions approach (already handled on your end)

**Deliverables**

* 1-page UX spec (happy path + error states)
* Acceptance criteria checklist for dev/testing

---

## Phase 1 — Foundations: service + models + DI (1–2 days)

**Goal:** Build the plumbing with zero UI changes yet.

**Work**

* Add `IPlacesAutocompleteService` + `PlacesAutocompleteService`
* Add DTO models for:

  * Predictions response
  * Place Details response (address + geometry)
* Add `HttpClient` setup + DI registration in `MauiProgram.cs`
* Add session token support and debounce/throttle logic (important)
* Add structured logging around:

  * request counts
  * latency
  * error types (quota/auth/network)

**Deliverables**

* New files compile + unit-testable service methods
* A simple “smoke test” method callable from debug UI or temporary page

**Acceptance criteria**

* Autocomplete returns predictions for a typed query
* Place Details returns formatted address + coordinates for a selected `place_id`

---

## Phase 2 — Reusable UI component: `LocationAutoCompleteView` (2–3 days)

**Goal:** Create a drop-in component the pages can reuse without rewriting flows.

**Work**

* Build `LocationAutoCompleteView` + ViewModel:

  * `SearchText`
  * `Predictions`
  * `IsBusy`
  * `ErrorMessage`
  * `SessionToken` per interaction
* Implement selection:

  * On item tap → call Place Details → emit `LocationSelected(Location)`
* Add UX polish:

  * debounce typing
  * minimum input length (e.g., 3 chars)
  * clear button + selection confirmation

**Deliverables**

* Component works in isolation on a dev test page

**Acceptance criteria**

* Typing shows suggestions; selecting one yields a `Location` with label/address/lat/lng
* Handles network failures gracefully (no crash; user can keep typing)

---

## Phase 3 — Integrate into Quote flow (1–2 days)

**Goal:** First production value: improve Quote pickup/dropoff without breaking anything.

**Work**

* Update `QuotePage` (and XAML) to use `LocationAutoCompleteView` for:

  * Pickup new location
  * Dropoff new location
* Keep old maps-based picker as a hidden/secondary fallback (feature flag or “Use map instead” link)

**Deliverables**

* Quote flow supports in-app autocomplete end-to-end

**Acceptance criteria**

* Quote pickup/dropoff can be completed without leaving the app
* Returned values correctly populate existing fields and downstream logic

---

## Phase 4 — Integrate into Book Ride flow (1–2 days)

**Goal:** Extend the same UX to the booking flow.

**Work**

* Update `BookRidePage` pickup/dropoff selection to use the component
* Verify any differences:

  * multi-step form state
  * existing label/address binding
  * any validation rules or formatting expectations

**Deliverables**

* Booking flow uses autocomplete for pickup/dropoff

**Acceptance criteria**

* Booking works without external Maps app
* Addresses persist correctly through page navigation within the flow

---

## Phase 5 — Lifecycle + resilience hardening (1–2 days)

**Goal:** Make the flow “unbreakable” even if the OS suspends the app.

**Work**

* Persist form state (lightweight):

  * selected pickup/dropoff (place_id, address, lat/lng)
  * in-progress text
  * current step in the form
* Restore on resume:

  * repopulate UI fields
  * recover typed text and selected values
* Add “manual entry” fallback mode if Places is down/quota exceeded

**Deliverables**

* Persistence + restore behavior verified on Android and iOS

**Acceptance criteria**

* Switching apps and returning does not wipe the form
* If the app is killed, reopening resumes to the last stable state or offers restore prompt

---

## Phase 6 — Cleanup + deprecation of external map picker (½–1 day)

**Goal:** Reduce code complexity and remove the old pain point.

**Work**

* Demote `PickLocationAsync` maps-launch flow to:

  * “View in Maps”
  * “Directions”
  * emergency fallback (optional)
* Clean up dialogs that ask users to manually type after returning from Maps
* Update docs and dev notes

**Deliverables**

* Simplified `LocationPickerService` responsibility: open maps/directions + geocode utilities

---

## Phase 7 — Instrumentation + quota protection (½–1 day)

**Goal:** Confirm real usage stays under free tier and avoid surprise costs.

**Work**

* Track:

  * autocomplete session counts
  * place details calls
  * errors per day
* Add guardrails:

  * local rate limiting
  * “no more calls today” UX if quota hit
* Confirm Cloud Console quotas are set

**Deliverables**

* A simple internal usage report (log-based) for the first month

---

### Suggested rollout strategy

* **Ship Phase 3 first** (Quote flow) behind a simple feature flag.
* Expand to **Phase 4** (Book Ride).
* Then harden with **Phase 5** and retire old behavior in **Phase 6**.
