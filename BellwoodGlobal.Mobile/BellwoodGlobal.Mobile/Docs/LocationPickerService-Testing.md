# Location Picker Service - Testing Guide

## Overview

This document provides comprehensive testing procedures for the Location Picker Service across all supported platforms.

## Prerequisites

### Development Environment
- Visual Studio 2022 (17.8+) with .NET MAUI workload
- Android SDK (API 21+)
- Xcode (for iOS/macOS testing)
- Windows 10/11 SDK

### Test Devices/Emulators
- Android Emulator with Google Play Services
- iOS Simulator or physical device
- Windows 10/11 machine
- macOS machine (for Mac Catalyst)

## Test Categories

### 1. Unit Tests

#### Location Model Tests

```csharp
[TestClass]
public class LocationModelTests
{
    [TestMethod]
    public void HasCoordinates_WithBothValues_ReturnsTrue()
    {
        var location = new Location
        {
            Latitude = 41.8781,
            Longitude = -87.6298
        };
        
        Assert.IsTrue(location.HasCoordinates);
    }

    [TestMethod]
    public void HasCoordinates_WithNullLatitude_ReturnsFalse()
    {
        var location = new Location
        {
            Latitude = null,
            Longitude = -87.6298
        };
        
        Assert.IsFalse(location.HasCoordinates);
    }

    [TestMethod]
    public void ToString_WithLabel_ReturnsLabelAndAddress()
    {
        var location = new Location
        {
            Label = "Home",
            Address = "123 Main St"
        };
        
        Assert.AreEqual("Home - 123 Main St", location.ToString());
    }

    [TestMethod]
    public void ToString_WithoutLabel_ReturnsAddressOnly()
    {
        var location = new Location
        {
            Label = "",
            Address = "123 Main St"
        };
        
        Assert.AreEqual("123 Main St", location.ToString());
    }

    [TestMethod]
    public void Coordinates_WithValidValues_ReturnsTuple()
    {
        var location = new Location
        {
            Latitude = 41.8781,
            Longitude = -87.6298
        };
        
        var coords = location.Coordinates;
        
        Assert.IsNotNull(coords);
        Assert.AreEqual(41.8781, coords.Value.Lat);
        Assert.AreEqual(-87.6298, coords.Value.Lng);
    }
}
```

#### LocationPickerResult Tests

```csharp
[TestClass]
public class LocationPickerResultTests
{
    [TestMethod]
    public void Cancelled_ReturnsCorrectState()
    {
        var result = LocationPickerResult.Cancelled();
        
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.WasCancelled);
        Assert.IsNull(result.Location);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Failed_ReturnsCorrectState()
    {
        var result = LocationPickerResult.Failed("Test error");
        
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.WasCancelled);
        Assert.IsNull(result.Location);
        Assert.AreEqual("Test error", result.ErrorMessage);
    }

    [TestMethod]
    public void Succeeded_ReturnsCorrectState()
    {
        var location = new Location { Label = "Test" };
        var result = LocationPickerResult.Succeeded(location);
        
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.WasCancelled);
        Assert.IsNotNull(result.Location);
        Assert.IsNull(result.ErrorMessage);
    }
}
```

### 2. Integration Tests

#### Test Checklist

| Test ID | Description | Steps | Expected Result |
|---------|-------------|-------|-----------------|
| LOC-001 | Pick location manually | 1. Tap "New Location"<br>2. Tap "??? Pick from Maps"<br>3. Select "?? Enter Address Manually"<br>4. Enter address<br>5. Enter label | Address and label populated in form |
| LOC-002 | Use current location | 1. Tap "New Location"<br>2. Tap "??? Pick from Maps"<br>3. Select "?? Use Current Location"<br>4. Grant permission if prompted<br>5. Enter label | Form populated with current address |
| LOC-003 | Open maps app | 1. Tap "New Location"<br>2. Tap "??? Pick from Maps"<br>3. Select "??? Open Maps App"<br>4. Browse in maps app<br>5. Return to app<br>6. Enter address from maps | Maps app opens, address can be entered |
| LOC-004 | Cancel operation | 1. Tap "New Location"<br>2. Tap "??? Pick from Maps"<br>3. Tap "Cancel" | Form unchanged, no error |
| LOC-005 | Permission denied | 1. Deny location permission<br>2. Tap "?? Use Current Location" | User-friendly error message shown |
| LOC-006 | Geocoding success | 1. Enter valid address<br>2. Save location | Coordinates stored with location |
| LOC-007 | Geocoding failure | 1. Enter invalid address<br>2. Save location | Location saved without coordinates |

### 3. Platform-Specific Tests

#### Android Tests

| Test ID | Description | Steps | Expected Result |
|---------|-------------|-------|-----------------|
| AND-001 | Google Maps launch | Open maps via service | Google Maps opens |
| AND-002 | Google Maps navigation | Request directions | Navigation mode starts |
| AND-003 | Permission request | First location request | System permission dialog appears |
| AND-004 | Location cache | Request location twice within 2 min | Second request uses cache |

#### iOS Tests

| Test ID | Description | Steps | Expected Result |
|---------|-------------|-------|-----------------|
| IOS-001 | Apple Maps launch | Open maps via service | Apple Maps opens |
| IOS-002 | Apple Maps directions | Request directions | Navigation mode starts |
| IOS-003 | Permission request | First location request | System permission dialog appears |
| IOS-004 | Background location | App backgrounded during maps | Returns correctly to app |

#### Windows Tests

| Test ID | Description | Steps | Expected Result |
|---------|-------------|-------|-----------------|
| WIN-001 | Bing Maps launch | Open maps via service | Bing Maps opens |
| WIN-002 | Bing Maps directions | Request directions | Directions shown |
| WIN-003 | Location services off | Disable Windows location | Graceful fallback |

### 4. UI/UX Tests

#### Visual Tests

| Test ID | Description | Verification |
|---------|-------------|--------------|
| UI-001 | Button visibility | "Pick from Maps" button visible when "New Location" selected |
| UI-002 | Button styling | Button has gold border, charcoal background |
| UI-003 | Action sheet | All options displayed correctly |
| UI-004 | Form population | Label and address fields populated after selection |
| UI-005 | Error display | Error alerts display correctly |

#### Accessibility Tests

| Test ID | Description | Verification |
|---------|-------------|--------------|
| A11Y-001 | Screen reader | Button announced correctly |
| A11Y-002 | Touch target | Button meets 44pt minimum |
| A11Y-003 | Color contrast | Text readable against background |

### 5. Edge Case Tests

| Test ID | Scenario | Expected Behavior |
|---------|----------|-------------------|
| EDGE-001 | No network connection | Geocoding fails gracefully, manual entry works |
| EDGE-002 | Maps app not installed | Falls back to web browser |
| EDGE-003 | GPS disabled | Shows appropriate error message |
| EDGE-004 | Empty address entered | Validation prevents save |
| EDGE-005 | Very long address | Text truncated appropriately |
| EDGE-006 | Special characters in address | Handled correctly |
| EDGE-007 | International address | Geocoding works globally |
| EDGE-008 | Rapid successive calls | Caching prevents excessive API calls |

## Manual Testing Procedure

### Pre-Test Setup

1. **Clean Install**
   ```bash
   # Android
   adb uninstall com.bellwoodglobal.mobile
   
   # Then deploy fresh build
   ```

2. **Clear Location Data**
   - Reset location permissions
   - Clear app data/cache

3. **Verify Network**
   - Ensure stable internet connection
   - Test with and without Wi-Fi

### Test Execution

#### Test Case: Complete Pickup Location Flow

**Objective:** Verify end-to-end pickup location selection using maps

**Prerequisites:**
- App installed and logged in
- Location permissions not yet granted

**Steps:**

1. Navigate to Quote Page or Book Ride Page
2. Observe pickup location picker shows saved locations
3. Select "New Location" from picker
4. Verify new location form appears
5. Tap "??? Pick from Maps" button
6. Verify action sheet appears with options:
   - ?? Enter Address Manually
   - ??? Open Maps App
   - ?? Use Current Location
   - Cancel

7. **Test Manual Entry:**
   - Select "?? Enter Address Manually"
   - Enter "Willis Tower, Chicago, IL"
   - Enter label "Willis Tower"
   - Verify form populated

8. **Test Current Location:**
   - Tap "??? Pick from Maps" again
   - Select "?? Use Current Location"
   - Grant location permission when prompted
   - Enter label when prompted
   - Verify address auto-populated

9. **Test Maps App:**
   - Tap "??? Pick from Maps" again
   - Select "??? Open Maps App"
   - Verify native maps opens
   - Return to app
   - Enter address from maps
   - Verify form populated

10. Tap "Save Pickup Location"
11. Verify location added to picker
12. Verify confirmation dialog shown

**Expected Results:**
- All options work correctly
- Forms populate with entered/detected data
- Coordinates stored when geocoding successful
- User experience smooth and intuitive

### Regression Testing

After any changes to the location picker system, run:

1. All LOC-* tests
2. Platform-specific tests for target platform
3. UI tests
4. Critical edge cases (EDGE-001, EDGE-002, EDGE-005)

## Performance Testing

### Metrics to Monitor

| Metric | Target | Test Method |
|--------|--------|-------------|
| Location fetch time | < 10 seconds | Stopwatch from tap to result |
| Geocoding time | < 3 seconds | Measure GeocodeAddressAsync |
| Cache hit rate | > 90% for repeated calls | Debug logging |
| Memory usage | No leaks | Profile during extended use |
| Battery impact | Minimal | Monitor during location use |

### Load Testing

```csharp
// Simulate multiple location requests
[TestMethod]
public async Task MultipleRapidRequests_UsesCacheEffectively()
{
    var service = new LocationPickerService();
    var sw = Stopwatch.StartNew();
    
    // First request (no cache)
    var loc1 = await service.GetCurrentLocationAsync();
    var firstTime = sw.ElapsedMilliseconds;
    
    sw.Restart();
    
    // Second request (should use cache)
    var loc2 = await service.GetCurrentLocationAsync();
    var secondTime = sw.ElapsedMilliseconds;
    
    Assert.IsTrue(secondTime < firstTime / 2, 
        "Cached request should be significantly faster");
}
```

## Troubleshooting

### Common Issues

| Issue | Possible Cause | Solution |
|-------|---------------|----------|
| "Location Unavailable" | GPS disabled | Enable location services |
| Maps app doesn't open | URL scheme issue | Check platform-specific handling |
| Geocoding fails | Network issue | Check connectivity, try again |
| Permission never asked | Already denied | Reset app permissions |
| Coordinates always null | Geocoding service down | Manual coordinates entry |

### Debug Logging

Enable debug logging to troubleshoot issues:

```csharp
#if DEBUG
System.Diagnostics.Debug.WriteLine($"[LocationPickerService] {message}");
#endif
```

Log locations:
- Permission request/result
- Geocoding request/result
- Cache hits/misses
- Map URL generation
- Error conditions

## Test Report Template

```
# Location Picker Test Report

**Date:** YYYY-MM-DD
**Tester:** Name
**Platform:** Android/iOS/Windows/macOS
**Build Version:** X.Y.Z

## Test Results Summary

| Category | Passed | Failed | Blocked |
|----------|--------|--------|---------|
| Unit Tests | X | X | X |
| Integration Tests | X | X | X |
| Platform Tests | X | X | X |
| UI Tests | X | X | X |
| Edge Cases | X | X | X |

## Failed Tests

| Test ID | Description | Actual Result | Notes |
|---------|-------------|---------------|-------|
| XXX-YYY | ... | ... | ... |

## Recommendations

1. ...
2. ...

## Sign-off

- [ ] All critical tests passing
- [ ] No blocking issues
- [ ] Ready for release
```

## Automated Test Setup

### Running Tests

```bash
# Unit tests
dotnet test BellwoodGlobal.Mobile.Tests

# UI tests (requires device/emulator)
dotnet test BellwoodGlobal.Mobile.UITests
```

### CI/CD Integration

```yaml
# Azure DevOps / GitHub Actions
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration Release'
```
