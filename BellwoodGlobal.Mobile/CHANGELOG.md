# Changelog

All notable changes to this solution will be documented in this file.

## [v0.2.0-phase2-mvp-lock] - 2025-10-18

### Added
- **Quote module flight logic (per-leg):**  
  - Commercial flights require a distinct return flight number.  
  - Private flights allow optional “change aircraft for return” (new tail number).
- **Airport pickup styles & signage:**  
  - Curbside vs Meet & Greet, with sign text capture.  
  - Non-airport pickups: Meet & Greet available via “Additional Requests,” with sign text field.
- **Form state ? draft mapping:** Introduced `QuoteFormState` and `QuoteDraftBuilder` to produce final `QuoteDraft`.
- **Persistence of new entries:**  
  - “Add New” Passenger and Location now save via `ProfileService` (backed by `Preferences`).
- **Admin API client:** Configured named `HttpClient` (`admin`) with emulator/desktop base addresses and DEBUG cert bypass.
- **Email payload readiness:** Quote JSON shown in-app and sent to Admin API for email dispatch.

### Changed
- Passenger phone/email no longer default to booker when left blank.
- Round-trip UX: return date/time validation and smart suggestions.
- QuotePage UI: refined sections for pickup/return, flight info, “change aircraft,” meet & greet, and JSON viewer.

### Fixed
- “Save New” buttons only show when creating new passenger/location and no longer overlap inputs.
- Minor null-safety and validation polish.

### Files of note
- `Pages/QuotePage.xaml` / `QuotePage.xaml.cs`
- `Models/QuoteFormState.cs`
- `Models/QuoteDraft.cs` (extended)
- `Models/PickupStyle.cs` (enum)
- `Services/QuoteDraftBuilder.cs` (new)
- `Services/ProfileService.cs`, `IProfileService.cs` (persistence updates)
- `MauiProgram.cs` (admin client wiring)

---
