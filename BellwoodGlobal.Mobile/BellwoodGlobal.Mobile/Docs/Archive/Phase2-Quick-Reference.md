# Phase 2 Quick Reference - LocationAutocompleteView

---

## ?? Quick Start

### Add to XAML
```xml
<components:LocationAutocompleteView 
    x:Name="MyAutocomplete"
    Placeholder="Search for an address..."
    LocationSelected="OnLocationSelected" />
```

### Handle Selection
```csharp
private void OnLocationSelected(object? sender, LocationSelectedEventArgs e)
{
    var location = e.Location;
    Console.WriteLine($"Selected: {location.Address}");
    Console.WriteLine($"Coords: {location.Latitude}, {location.Longitude}");
}
```

---

## ?? Testing Checklist

- [ ] Run app ? Login
- [ ] Tap "?? Test Autocomplete Component (Phase 2)"
- [ ] Type "123 Main" in Pickup ? See predictions
- [ ] Tap first prediction ? See location details populate
- [ ] Alert shows ? Component clears ? Ready for next search
- [ ] Repeat for Dropoff
- [ ] Try gibberish query ? See error message
- [ ] Turn off WiFi ? See offline error
- [ ] Tap "Clear Both" ? Both reset

---

## ? Acceptance Criteria

| Criterion | Status |
|-----------|--------|
| Typing shows suggestions | ? |
| Selecting yields Location | ? |
| Network failures graceful | ? |
| Debouncing (300ms) | ? |
| Min 3 characters | ? |
| Clear button works | ? |
| Works in isolation | ? |

**All 7 met!** ?

---

## ?? Next: Phase 3

**Goal:** Integrate into QuotePage

**Tasks:**
1. Replace "Pick from Maps" with component
2. Wire up selection to existing logic
3. Keep manual entry as fallback
4. Test end-to-end quote submission

**Estimated:** 2-3 hours

---

## ?? Access Test Page

MainPage ? Tap **"?? Test Autocomplete Component (Phase 2)"**

---

**Phase 2 Status:** ? **COMPLETE**
