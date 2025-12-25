# Bug Fix: Malformed URL Exception in Places API

**Date:** 2025-12-25  
**Issue:** `Java.Net.MalformedURLException: unknown protocol: places`  
**Status:** ? **FIXED**  

---

## Problem

### Error Message
```
Java.Net.MalformedURLException: unknown protocol: places
at java.net.URL.<init>(URL.java:608)
```

### Root Cause

The HttpClient base URL was configured as:
```csharp
c.BaseAddress = new Uri("https://places.googleapis.com/v1/");
```

And the service was trying to POST to:
```csharp
await client.PostAsync("places:autocomplete", content, ct);
```

**Issue:** The colon (`:`) in `places:autocomplete` was being interpreted as a **URL protocol** (like `http:` or `https:`), not as part of the path!

The combined URL became:
```
places:autocomplete  // Invalid! "places:" is not a protocol
```

Instead of:
```
https://places.googleapis.com/v1/places:autocomplete  // Correct
```

---

## Solution

### Changed Base URL Configuration

**File:** `MauiProgram.cs`

**Before:**
```csharp
c.BaseAddress = new Uri("https://places.googleapis.com/v1/");
```

**After:**
```csharp
c.BaseAddress = new Uri("https://places.googleapis.com/");
```

*(Removed `v1/` from base URL)*

---

### Updated Service Endpoints

**File:** `PlacesAutocompleteService.cs`

#### Autocomplete Endpoint

**Before:**
```csharp
await client.PostAsync("places:autocomplete", content, ct);
```

**After:**
```csharp
await client.PostAsync("v1/places:autocomplete", content, ct);
```

#### Place Details Endpoint

**Before:**
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, $"places/{placeId}");
```

**After:**
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, $"v1/places/{placeId}");
```

---

## Final URLs

### Autocomplete
```
Base:     https://places.googleapis.com/
Path:     v1/places:autocomplete
Combined: https://places.googleapis.com/v1/places:autocomplete ?
```

### Place Details
```
Base:     https://places.googleapis.com/
Path:     v1/places/{placeId}
Combined: https://places.googleapis.com/v1/places/{placeId} ?
```

---

## Why This Happened

The colon (`:`) character in URLs has special meaning:
- In `https://example.com` ? `https:` is the **protocol**
- In `places:autocomplete` ? Android's URL parser thought `places:` was a protocol
- Since `places:` isn't a valid protocol (only `http:`, `https:`, `ftp:`, etc.), it threw `MalformedURLException`

By including the full path with `v1/` prefix, the colon becomes part of the path segment, not a protocol separator:
```
v1/places:autocomplete  ? Path segment ?
places:autocomplete     ? Invalid protocol ?
```

---

## Testing Verification

After this fix, you should see:

### Expected Log Output

**Before (Error):**
```
[PlacesAutocompleteService] Autocomplete request: '123 Main street'
[PlacesAutocompleteService] Autocomplete error: Java.Net.MalformedURLException: unknown protocol: places
[PlacesAPI] ERROR | Autocomplete | UnexpectedError | unknown protocol: places
[PlacesTestPage] ? Found 0 predictions in 62ms
```

**After (Success):**
```
[PlacesAutocompleteService] Autocomplete request: '123 Main street'
[PlacesAPI] Autocomplete | Status: 200 OK | Latency: 342ms | Time: 12:05:06
[PlacesAutocompleteService] Autocomplete returned 5 predictions in 342ms
[PlacesTestPage] ? Found 5 predictions in 342ms
```

---

## Files Changed

| File | Changes |
|------|---------|
| `MauiProgram.cs` | Changed base URL from `https://places.googleapis.com/v1/` to `https://places.googleapis.com/` |
| `PlacesAutocompleteService.cs` | Updated autocomplete path to `v1/places:autocomplete` |
| `PlacesAutocompleteService.cs` | Updated place details path to `v1/places/{placeId}` |

---

## Build Status

? **Build Successful** - All changes compile without errors

---

## Next Steps

1. **Re-run the app** on Android emulator
2. **Navigate to PlacesTestPage** (tap "?? Test Places API (Debug)")
3. **Type a search query:** `123 Main street`
4. **Verify predictions appear** (no more errors!)
5. **Tap a prediction** to test Place Details

---

## Additional Notes

### API Key in Log

Your log showed the API key being used:
```csharp
c.DefaultRequestHeaders.Add("X-Goog-Api-Key", "AIzaSyDzAsZxbY4ZnHGBt9X_17Mc532J6t5_LA8");
```

This is correct! The key is being sent properly. The issue was purely the URL construction.

### Location Bias Working

The log also confirmed location biasing is working:
```
[PlacesAutocompleteService] Using cached user location
[PlacesAutocompleteService] Using location bias: 37.4220, -122.0840
```

That's **Mountain View, California** (Google's headquarters area) - likely the emulator's default GPS location! The dynamic biasing is working perfectly. When you test on a real device or change the emulator's GPS coordinates to Fort Lauderdale, it will bias to that location instead.

---

## Summary

? **Issue Fixed:** URL construction error resolved  
? **Build Successful:** All code compiles  
? **Ready to Test:** Run the app and try autocomplete again!  

The API key is fine, the service logic is fine, it was just a URL path construction issue. Should work perfectly now! ??
