# Troubleshooting

**Document Type**: Living Document - Operations & Support  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides solutions for common issues encountered when developing, testing, or deploying the Bellwood Global Mobile App. Issues are organized by category for quick reference.

**Categories**:
- ?? **Build & Deployment** - Compilation, packaging, publishing
- ?? **Authentication & Security** - Login, tokens, permissions
- ?? **Network & API** - Connectivity, endpoints, timeouts
- ?? **Google Places** - Autocomplete, API keys, quotas
- ?? **Data & State** - Quotes, bookings, data sync
- ??? **Location Tracking** - GPS, maps, driver tracking
- ?? **Configuration** - Settings, environment variables

---

## ?? Build & Deployment Issues

### Issue: "MAUI workload not found"

**Symptoms**:
- Build error: "The MAUI workload is not installed"
- Cannot build for Android/iOS

**Solution**:
```bash
# Install MAUI workload
dotnet workload install maui

# Verify installation
dotnet workload list
# Expected: maui, android, ios, maccatalyst
```

---

### Issue: "Android SDK not found"

**Symptoms**:
- Build error: "Android SDK not found"
- Cannot build for Android

**Solution**:
```bash
# Set ANDROID_HOME environment variable
# Windows
$env:ANDROID_HOME = "C:\Program Files\Android\android-sdk"

# Linux/macOS
export ANDROID_HOME="/path/to/android-sdk"

# Verify
echo $ANDROID_HOME
```

---

### Issue: "iOS build requires macOS"

**Symptoms**:
- Cannot build for iOS on Windows/Linux
- Error: "iOS builds only supported on macOS"

**Solution**:
- iOS builds require macOS with Xcode
- Use GitHub Actions with `macos-latest` runner
- Or use a Mac for iOS development

---

### Issue: "appsettings.json not found at runtime"

**Symptoms**:
- App crashes on startup
- Error: "Could not load appsettings.json"

**Solution**:
```xml
<!-- BellwoodGlobal.Mobile.csproj -->
<ItemGroup>
  <MauiAsset Include="appsettings.json" />
  <MauiAsset Include="appsettings.Development.json" />
</ItemGroup>
```

Rebuild app after adding.

---

## ?? Authentication & Security Issues

### Issue: "401 Unauthorized" on API calls

**Symptoms**:
- API requests return 401
- Error: "Authorization header missing or invalid"

**Possible Causes**:
1. JWT token expired
2. Token not included in request
3. Invalid token

**Solution**:

**1. Check token expiration**:
```csharp
var token = await SecureStorage.GetAsync("access_token");
var handler = new JwtSecurityTokenHandler();
var jwt = handler.ReadJwtToken(token);

if (jwt.ValidTo < DateTime.UtcNow)
{
    // Token expired - re-login or refresh
    await RefreshTokenAsync();
}
```

**2. Verify token in request**:
```csharp
// Ensure Authorization header is set
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

**3. Re-login if refresh fails**:
```csharp
await Shell.Current.GoToAsync("//LoginPage");
```

---

### Issue: "403 Forbidden" accessing quote/booking

**Symptoms**:
- API returns 403 Forbidden
- Error: "You don't have permission to access this resource"

**Possible Causes**:
1. User trying to access another user's data
2. Email doesn't match booking passenger email (driver tracking)

**Solution**:

**1. Verify ownership**:
```csharp
// Backend checks CreatedByUserId matches JWT uid claim
// Users can only access their own quotes/bookings
```

**2. For driver tracking**:
```csharp
// JWT email must match booking passenger email
var userEmail = User.FindFirst("email")?.Value;
var booking = await GetBookingAsync(rideId);

if (userEmail != booking.Draft.Passenger.EmailAddress)
{
    // Not authorized
}
```

---

### Issue: "Credentials stored in plaintext"

**Symptoms**:
- Passwords visible in Preferences
- Security audit failure

**Solution**:

**Never store passwords**:
```csharp
// ? BAD
Preferences.Set("password", password);

// ? GOOD - Only store tokens
await SecureStorage.SetAsync("access_token", token);
```

**Clear sensitive data on logout**:
```csharp
SecureStorage.Remove("access_token");
SecureStorage.Remove("refresh_token");
Preferences.Clear();
```

---

## ?? Network & API Issues

### Issue: "Cannot connect to AdminAPI"

**Symptoms**:
- Network error on API calls
- Timeout exceptions

**Solution**:

**1. Verify AdminAPI is running**:
```powershell
# Test AdminAPI health
curl https://localhost:5206/health
# Expected: 200 OK
```

**2. Start AdminAPI if down**:
```bash
cd AdminAPI
dotnet run
```

**3. Check configuration**:
```csharp
// Verify AdminApiUrl is correct
var config = await _configService.GetAdminApiUrlAsync();
Console.WriteLine($"AdminAPI URL: {config}");
// Expected: https://localhost:5206 (dev) or https://api.bellwood.com (prod)
```

---

### Issue: "SSL certificate validation failed"

**Symptoms**:
- HTTPS requests fail
- Error: "The SSL connection could not be established"

**Solution**:

**Development (localhost)**:
```bash
# Trust development certificate
dotnet dev-certs https --trust
```

**Production**:
- Verify SSL certificate is valid
- Check certificate expiration
- Ensure certificate chain is complete

---

### Issue: "Request timeout"

**Symptoms**:
- API calls take too long
- Timeout exceptions after 30+ seconds

**Solution**:

**1. Increase timeout**:
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(60);
```

**2. Check network connectivity**:
```csharp
var connectivity = Connectivity.Current.NetworkAccess;
if (connectivity != NetworkAccess.Internet)
{
    await DisplayAlert("No Internet", "Check your network connection", "OK");
}
```

**3. Retry transient failures**:
```csharp
public async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException) when (i < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
    return await operation();
}
```

---

## ?? Google Places Issues

### Issue: "No autocomplete predictions appear"

**Symptoms**:
- Typing in search box shows no suggestions
- Empty prediction list

**Possible Causes**:
1. API key not configured
2. API key invalid
3. Network error
4. Quota exceeded

**Solution**:

**1. Verify API key configured**:
```csharp
var apiKey = await _config.GetGooglePlacesApiKeyAsync();
Console.WriteLine($"API Key: {apiKey.Substring(0, 10)}...");
// Should not be null or "ENV:..."
```

**2. Check API key restrictions**:
- Google Cloud Console ? Credentials
- Verify package name (Android) or bundle ID (iOS) matches
- Verify SHA-1 fingerprint (Android) is correct
- Verify Places API (New) is enabled

**3. Check quota**:
```csharp
var quotaUsed = Preferences.Get("PlacesQuotaUsedToday", 0);
Console.WriteLine($"Quota used: {quotaUsed} / 2500");
```

---

### Issue: "REQUEST_DENIED" from Google Places API

**Symptoms**:
- 403 Forbidden from Google
- Error message: "REQUEST_DENIED"

**Cause**: API key restrictions don't match app

**Solution**:

**Android**:
1. Google Cloud Console ? Credentials ? Edit API Key
2. Application restrictions ? Android apps
3. Package name: `com.bellwood.mobile`
4. SHA-1 fingerprint: Get from project settings
   ```bash
   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey
   ```
5. Copy SHA-1 fingerprint to API key restrictions

**iOS**:
1. Application restrictions ? iOS apps
2. Bundle ID: `com.bellwood.mobile`

**API Restrictions**:
- Restrict to: Places API (New)

---

### Issue: "Quota exceeded"

**Symptoms**:
- Autocomplete stops working mid-day
- Error: "Quota exceeded"

**Cause**: Exceeded free tier (2,500 requests/day)

**Solution**:

**1. Check quota usage**:
```csharp
var quotaUsed = Preferences.Get("PlacesQuotaUsedToday", 0);
```

**2. Wait for quota reset** (midnight UTC):
```csharp
var lastReset = Preferences.Get("PlacesQuotaResetDate", DateTime.MinValue);
var nextReset = lastReset.Date.AddDays(1);
Console.WriteLine($"Quota resets at: {nextReset} UTC");
```

**3. Increase quota** (requires billing in Google Cloud Console):
- Google Cloud Console ? APIs & Services ? Places API (New)
- Quotas ? Increase limit

---

## ?? Data & State Issues

### Issue: "Quotes not appearing in dashboard"

**Symptoms**:
- Quote list is empty
- "No quotes found" message

**Possible Causes**:
1. User hasn't submitted quotes
2. Wrong user logged in
3. API filtering by wrong user ID
4. Network error

**Solution**:

**1. Verify user logged in**:
```csharp
var token = await SecureStorage.GetAsync("access_token");
if (string.IsNullOrEmpty(token))
{
    // Not logged in
    await Shell.Current.GoToAsync("//LoginPage");
}
```

**2. Verify quotes exist for user**:
```powershell
# Use test script to check backend
.\Scripts\Get-QuoteInfo.ps1
```

**3. Check API response**:
```csharp
var quotes = await _adminApi.GetQuotesAsync();
Console.WriteLine($"Received {quotes.Count} quotes");
```

**4. Pull-to-refresh**:
```csharp
// Try manual refresh
await RefreshQuotesAsync();
```

---

### Issue: "Quote status not updating"

**Symptoms**:
- Quote stays in old status
- Polling not working

**Possible Causes**:
1. Polling timer stopped
2. Page not active
3. Network error

**Solution**:

**1. Verify polling active**:
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    
    // Restart polling
    _pollingTimer?.Start();
    await RefreshQuotesAsync();
}
```

**2. Check timer interval**:
```csharp
var interval = _config.QuotePollingIntervalSeconds * 1000;
Console.WriteLine($"Polling every {interval}ms");
// Expected: 30000 (30 seconds)
```

**3. Manual refresh**:
```csharp
// Pull-to-refresh
await RefreshQuotesAsync();
```

---

### Issue: "Cannot accept quote" (400 Bad Request)

**Symptoms**:
- "Cannot accept quote" error
- 400 Bad Request from API

**Possible Causes**:
1. Quote not in "Responded" status
2. Quote already accepted
3. Quote cancelled

**Solution**:

**1. Check quote status**:
```powershell
.\Scripts\Get-QuoteInfo.ps1 -QuoteId "quote-123"
```

**2. Ensure status is "Responded"**:
```csharp
if (quote.Status != "Responded")
{
    await DisplayAlert(
        "Cannot Accept",
        $"Quote must be in 'Responded' status. Current status: {quote.Status}",
        "OK");
}
```

**3. Refresh quote details**:
```csharp
// Get latest status
var quote = await _adminApi.GetQuoteAsync(quoteId);
```

---

## ??? Location Tracking Issues

### Issue: "Driver hasn't started trip yet"

**Symptoms**:
- Tracking page shows "not started" message
- No driver marker on map

**Cause**: Driver hasn't begun location tracking (normal behavior)

**Solution**:
- **This is expected** - driver will start tracking when they begin trip
- Polling continues automatically
- Page updates when driver starts

---

### Issue: "Not authorized to view this ride"

**Symptoms**:
- 403 Forbidden from tracking endpoint
- "Unauthorized" message

**Cause**: User's email doesn't match booking passenger email

**Solution**:

**1. Verify user email**:
```csharp
var token = await SecureStorage.GetAsync("access_token");
var handler = new JwtSecurityTokenHandler();
var jwt = handler.ReadJwtToken(token);
var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

Console.WriteLine($"Logged in as: {email}");
```

**2. Verify booking passenger email**:
```csharp
var booking = await _adminApi.GetBookingAsync(bookingId);
Console.WriteLine($"Booking passenger: {booking.Draft.Passenger.EmailAddress}");
```

**3. Emails must match**:
- Login with passenger's email, OR
- Update booking passenger email to match current user

---

### Issue: "Driver marker not updating"

**Symptoms**:
- Marker stays in same position
- ETA not changing

**Possible Causes**:
1. Driver stopped sending updates
2. Polling stopped
3. Network issue

**Solution**:

**1. Verify polling active**:
```csharp
Console.WriteLine($"Polling active: {_trackingService.IsTracking}");
```

**2. Check last update timestamp**:
```csharp
var location = _trackingService.LastKnownLocation;
Console.WriteLine($"Last update: {location.Timestamp} ({location.AgeSeconds}s ago)");
```

**3. Restart tracking**:
```csharp
await _trackingService.StopTracking();
await _trackingService.StartTrackingAsync(rideId, pickupLat, pickupLng);
```

---

## ?? Configuration Issues

### Issue: "GooglePlacesApiKey not found"

**Symptoms**:
- Autocomplete doesn't work
- Error: "API key is null or empty"

**Cause**: `appsettings.Development.json` missing or ENV variable not set

**Solution**:

**Development**:
```bash
# Create appsettings.Development.json
cp appsettings.json appsettings.Development.json

# Edit file and replace ENV: placeholders
# GooglePlacesApiKey: "AIzaSy..." (actual key)
```

**Production**:
```bash
# Set environment variable
export GOOGLE_PLACES_API_KEY="AIzaSy..."

# Verify
echo $GOOGLE_PLACES_API_KEY
```

---

### Issue: "Environment variable not found"

**Symptoms**:
- Production app can't connect
- Error: "Environment variable 'ADMIN_API_URL' not found"

**Solution**:
```bash
# Set environment variable before running app
export ADMIN_API_URL="https://api.bellwood.com"
export AUTH_SERVER_URL="https://auth.bellwood.com"
export GOOGLE_PLACES_API_KEY="AIzaSy..."

# Verify
echo $ADMIN_API_URL
```

---

## ?? Debugging Tips

### Enable Debug Logging

```json
// appsettings.Development.json
{
  "EnableDebugLogging": true
}
```

```csharp
#if DEBUG
if (config.EnableDebugLogging)
{
    System.Diagnostics.Debug.WriteLine($"[QuoteDashboard] Refreshed: {quotes.Count} quotes");
}
#endif
```

---

### View API Responses

```csharp
var response = await _httpClient.GetAsync("/quotes/list");
var json = await response.Content.ReadAsStringAsync();
Console.WriteLine($"API Response: {json}");
```

---

### Check Token Claims

```csharp
var token = await SecureStorage.GetAsync("access_token");
var handler = new JwtSecurityTokenHandler();
var jwt = handler.ReadJwtToken(token);

foreach (var claim in jwt.Claims)
{
    Console.WriteLine($"{claim.Type}: {claim.Value}");
}
```

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing procedures
- **[10-Google-Places-Autocomplete.md](10-Google-Places-Autocomplete.md)** - Autocomplete troubleshooting
- **[11-Location-Tracking.md](11-Location-Tracking.md)** - Tracking troubleshooting
- **[20-API-Integration.md](20-API-Integration.md)** - API error handling
- **[22-Configuration.md](22-Configuration.md)** - Configuration setup
- **[23-Security-Model.md](23-Security-Model.md)** - Authentication issues
- **[30-Deployment-Guide.md](30-Deployment-Guide.md)** - Deployment issues
- **[31-Scripts-Reference.md](31-Scripts-Reference.md)** - Testing scripts

---

## ?? Getting Help

### Before Reporting an Issue

1. ? Check this troubleshooting guide
2. ? Review relevant feature documentation
3. ? Enable debug logging
4. ? Check API logs (if backend issue)
5. ? Verify environment health (`.\Scripts\Test-Environment.ps1`)

### Reporting a Bug

**Include**:
- Device/platform (iOS/Android/Windows)
- .NET version (`dotnet --version`)
- Steps to reproduce
- Error messages (full stack trace)
- Screenshots (if UI issue)
- Logs (with debug logging enabled)

**GitHub Issue Template**:
```markdown
## Bug Description
[Clear description]

## Steps to Reproduce
1. [Step 1]
2. [Step 2]
3. [Step 3]

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Environment
- Device: [iPhone 14 / Pixel 7 / Windows 11]
- OS: [iOS 17.2 / Android 14 / Windows 11]
- .NET: [9.0.1]

## Logs/Screenshots
[Attach logs or screenshots]
```

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
