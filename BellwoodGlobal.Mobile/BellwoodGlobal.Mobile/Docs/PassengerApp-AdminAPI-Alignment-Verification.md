# Passenger App - AdminAPI Alignment Verification

## ? Summary

The passenger app is **correctly aligned** with the AdminAPI's passenger-safe endpoint `/passenger/rides/{rideId}/location`.

---

## ?? Verification Results

### 1. Endpoint Usage ?

**AdminAPI Documentation:**
```http
GET /passenger/rides/{rideId}/location
Authorization: Bearer {jwt_token}
```

**Passenger App Implementation:**

#### DriverTrackingService.cs (Line 85)
```csharp
var response = await _http.GetAsync($"/passenger/rides/{Uri.EscapeDataString(rideId)}/location");
```
? **Correct endpoint**

#### AdminApi.cs (Updated)
```csharp
var response = await _http.GetAsync($"/passenger/rides/{Uri.EscapeDataString(rideId)}/location");
```
? **Now updated to match**

---

### 2. Authorization Flow ?

**Required:** JWT token with email claim in Authorization header

**Implementation:**

#### HTTP Client Configuration (MauiProgram.cs)
```csharp
builder.Services.AddHttpClient("admin", c =>
{
    c.BaseAddress = new Uri("https://localhost:5206");
    c.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<AuthHttpHandler>() // ? Attaches JWT token
```
? **AuthHttpHandler attached to "admin" client**

#### AuthHttpHandler.cs
```csharp
var token = await _auth.GetValidTokenAsync();
if (!string.IsNullOrWhiteSpace(token))
{
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
```
? **JWT token added to Authorization header**

#### AuthService.cs
```csharp
public async Task<string?> GetValidTokenAsync()
{
    var token = await GetTokenAsync();
    if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
        return null;
    return token;
}
```
? **Token retrieved from SecureStorage**

#### LoginPage.cs
```csharp
var res = await client.PostAsJsonAsync("/login", creds);
var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
await _auth.SetTokenAsync(body.Token);
```
? **Token received from AuthServer and stored**

---

### 3. Request Format ?

**AdminAPI Expects:**
- HTTP Method: GET
- URL: `/passenger/rides/{rideId}/location`
- Headers: `Authorization: Bearer {jwt_token}` (with email claim)
- Body: None (GET request)

**Passenger App Sends:**
- ? HTTP Method: GET
- ? URL: `/passenger/rides/{rideId}/location`
- ? Headers: `Authorization: Bearer {jwt_token}` (via AuthHttpHandler)
- ? Body: None

---

### 4. Response Handling ?

**AdminAPI Returns:**

#### Scenario 1: Tracking Active (200 OK)
```json
{
  "rideId": "abc123",
  "trackingActive": true,
  "latitude": 41.8781,
  "longitude": -87.6298,
  "timestamp": "2024-12-18T15:30:15Z",
  "heading": 45.5,
  "speed": 12.3,
  "ageSeconds": 5.2,
  "driverUid": "driver-001",
  "driverName": "Charlie Johnson"
}
```

#### Scenario 2: Tracking Not Started (200 OK)
```json
{
  "rideId": "abc123",
  "trackingActive": false,
  "message": "Driver has not started tracking yet",
  "currentStatus": "Scheduled"
}
```

#### Scenario 3: Unauthorized (403 Forbidden)
```json
{
  "error": "You can only view location for your own bookings"
}
```

**Passenger App Handles:**

#### DriverTrackingService.cs
```csharp
// 404 Not Found
if (response.StatusCode == HttpStatusCode.NotFound)
{
    return null;
}

// 403 Forbidden
if (response.StatusCode == HttpStatusCode.Forbidden)
{
    SetState(TrackingState.Unauthorized);
    return null;
}

// Parse PassengerLocationResponse
var passengerResponse = await response.Content
    .ReadFromJsonAsync<PassengerLocationResponse>(_jsonOptions);

// Check if tracking has started
if (!passengerResponse.TrackingActive)
{
    SetState(TrackingState.NotStarted);
    return null;
}

// Convert to DriverLocation
return passengerResponse.ToDriverLocation();
```
? **All scenarios handled correctly**

---

### 5. Model Alignment ?

**AdminAPI Response Model:**
```csharp
public class PassengerLocationResponse
{
    public string RideId { get; set; }
    public bool TrackingActive { get; set; }
    public string? Message { get; set; }
    public string? CurrentStatus { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? AgeSeconds { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
}
```

**Passenger App Model (DriverLocation.cs):**
```csharp
public sealed class PassengerLocationResponse
{
    public string RideId { get; set; } = "";
    public bool TrackingActive { get; set; }
    public string? Message { get; set; }
    public string? CurrentStatus { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double? AgeSeconds { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
    
    public DriverLocation? ToDriverLocation() { ... }
}
```
? **Model matches AdminAPI contract**

---

## ?? Critical Dependencies

### AuthServer Configuration ?

You mentioned that changes have been made to AuthServer:
1. ? Include email address in JWT token
2. ? Add Admin role to Alice and Bob logins

**Verification:** The JWT token must contain an `email` claim that matches either:
- `booking.Draft.Booker.EmailAddress`, OR
- `booking.Draft.Passenger.EmailAddress`

**Example JWT Payload:**
```json
{
  "sub": "user-123",
  "email": "alice@example.com",  ? Must match booking
  "role": "Admin",
  "exp": 1703001234
}
```

---

## ?? Testing Checklist

### Pre-Test Verification

**Backend (AdminAPI):**
- [ ] Endpoint `/passenger/rides/{rideId}/location` exists
- [ ] Email-based authorization implemented
- [ ] Returns `PassengerLocationResponse` format
- [ ] Handles tracking not started scenario
- [ ] Returns 403 for unauthorized access

**AuthServer:**
- [x] JWT tokens include `email` claim ? (confirmed by you)
- [x] Alice and Bob have Admin role ? (confirmed by you)
- [ ] Token expiration is reasonable (e.g., 1 hour)

**Passenger App:**
- [x] Uses `/passenger/rides/{rideId}/location` endpoint ?
- [x] Sends JWT token via AuthHttpHandler ?
- [x] Handles all response scenarios ?
- [x] Models match API contract ?

---

### Test Scenarios

#### ? TC1: Passenger Tracks Own Ride (Happy Path)

**Setup:**
1. Create booking as Alice (alice@example.com)
2. Assign driver to ride
3. Driver starts ride (status ? OnRoute)
4. Driver sends location updates

**Steps:**
1. Login as Alice in passenger app
2. Navigate to booking detail
3. Tap "Track Driver"

**Expected:**
- ? Map loads with pickup pin
- ? Loading overlay appears
- ? Within 15 seconds, driver marker appears
- ? Status chip shows "Live" (gold)
- ? ETA and distance displayed

**Console Logs:**
```
[AuthHttpHandler] Token retrieved for /passenger/rides/abc123/location: Present (length: 437)
[DriverTrackingService] Location received: 41.878100, -87.629800, Age=5s
[DriverTrackingService] State changed to: Tracking
[DriverTrackingService] ETA: 8 min away, Distance: 3.21 km
```

**API Request:**
```http
GET /passenger/rides/abc123/location
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**API Response:**
```json
{
  "rideId": "abc123",
  "trackingActive": true,
  "latitude": 41.8781,
  "longitude": -87.6298,
  "timestamp": "2024-12-18T15:30:15Z",
  "ageSeconds": 5
}
```

---

#### ? TC2: Tracking Not Started

**Setup:**
1. Create booking as Alice
2. Driver assigned but hasn't started yet

**Steps:**
1. Login as Alice
2. Tap "Track Driver"

**Expected:**
- ? Loading overlay appears
- ? Unavailable overlay shows
- ? Message: "Your driver hasn't started the trip yet"
- ? Status chip shows "Waiting" (orange)
- ? Continues polling every 15 seconds

**Console Logs:**
```
[DriverTrackingService] Tracking not started: Driver has not started tracking yet
[DriverTrackingService] State changed to: NotStarted
```

**API Response:**
```json
{
  "rideId": "abc123",
  "trackingActive": false,
  "message": "Driver has not started tracking yet",
  "currentStatus": "Scheduled"
}
```

---

#### ? TC3: Unauthorized Access

**Setup:**
1. Booking A belongs to Alice (alice@example.com)
2. Booking B belongs to Bob (bob@example.com)

**Steps:**
1. Login as Bob
2. Try to view Alice's booking tracking

**Expected:**
- ? Unavailable overlay shows
- ? Message: "You are not authorized to view this ride"
- ? Status chip shows "Error" (red)
- ? Retry button hidden
- ? Polling stops

**Console Logs:**
```
[DriverTrackingService] Forbidden: Not authorized to view ride alice-abc123
[DriverTrackingService] State changed to: Unauthorized
```

**API Response:**
```http
HTTP/1.1 403 Forbidden
{
  "error": "You can only view location for your own bookings"
}
```

---

#### ? TC4: Token Missing/Expired

**Setup:**
1. Clear SecureStorage or use expired token

**Steps:**
1. Try to view tracking page

**Expected:**
- ? AuthHttpHandler logs warning: "No valid token available"
- ? API returns 401 Unauthorized
- ? AuthHttpHandler triggers logout
- ? User redirected to LoginPage

**Console Logs:**
```
[AuthService] GetValidTokenAsync: Token is expired
[AuthHttpHandler] WARNING: No valid token available for /passenger/rides/abc123/location
[AuthHttpHandler] Response status: 401 Unauthorized
[AuthHttpHandler] 401 Unauthorized - triggering logout
[AuthService] LogoutAsync: Clearing token and navigating to login
```

---

## ?? Troubleshooting

### Issue: Still Getting 403 Forbidden

**Possible Causes:**
1. JWT token doesn't contain email claim
2. Email in token doesn't match booking
3. Booking doesn't have booker/passenger email set

**Solutions:**

1. **Verify JWT Token Contains Email:**
   - Decode token at https://jwt.io
   - Check for `email` claim
   - Verify email matches login

2. **Check Booking Data:**
   ```sql
   -- In AdminAPI database
   SELECT Id, BookerEmail, PassengerEmail FROM Bookings WHERE Id = 'abc123'
   ```

3. **Add Debug Logging (AdminAPI):**
   ```csharp
   var userEmail = User.FindFirst("email")?.Value;
   Console.WriteLine($"User email: {userEmail}");
   Console.WriteLine($"Booker email: {booking.Draft.Booker.EmailAddress}");
   Console.WriteLine($"Passenger email: {booking.Draft.Passenger.EmailAddress}");
   ```

---

### Issue: "Tracking Not Started" Forever

**Possible Causes:**
1. Driver app not sending location updates
2. Driver status not set to OnRoute
3. RideId mismatch

**Solutions:**

1. **Check Driver Status:**
   ```
   AdminAPI ? Check ride.CurrentStatus
   Should be: OnRoute, Dispatched, or EnRoute
   ```

2. **Verify Driver Location Updates:**
   ```
   Check AdminAPI logs for POST /driver/location/update
   ```

3. **Verify RideId:**
   ```
   Passenger app sends: "abc123"
   Backend expects: "abc123"
   Must match exactly
   ```

---

### Issue: No JWT Token Sent

**Possible Causes:**
1. Not logged in
2. Token expired
3. HTTP client not using AuthHttpHandler

**Solutions:**

1. **Verify Login:**
   ```
   Check SecureStorage for "access_token" key
   ```

2. **Verify HTTP Client:**
   ```csharp
   // DriverTrackingService should use "admin" client
   _http = httpFactory.CreateClient("admin");
   ```

3. **Check AuthHttpHandler Configuration:**
   ```csharp
   builder.Services.AddHttpClient("admin", c => { ... })
       .AddHttpMessageHandler<AuthHttpHandler>() // Must be present
   ```

---

## ?? Request/Response Flow

```
User Taps "Track Driver"
    ?
DriverTrackingService.StartTrackingAsync(rideId, lat, lng)
    ?
PollLocationAsync() runs every 15 seconds
    ?
GetDriverLocationAsync(rideId)
    ?
HTTP Client: "admin" (port 5206)
    ?
AuthHttpHandler intercepts request
    ?
AuthService.GetValidTokenAsync()
    ?
SecureStorage.GetAsync("access_token")
    ?
AuthHttpHandler adds header: Authorization: Bearer {token}
    ?
HTTP Request sent:
    GET /passenger/rides/abc123/location
    Authorization: Bearer eyJhbGci...
    ?
AdminAPI receives request
    ?
JWT middleware extracts email claim
    ?
AdminAPI loads booking from database
    ?
AdminAPI checks: userEmail == bookerEmail || userEmail == passengerEmail
    ?
If authorized:
    ??? Tracking active? Return location data
    ??? Tracking not started? Return TrackingActive: false
If not authorized:
    ??? Return 403 Forbidden
    ?
Passenger app receives response
    ?
DriverTrackingService handles response:
    ??? 200 OK + TrackingActive: true ? Update map
    ??? 200 OK + TrackingActive: false ? Show "not started" message
    ??? 403 Forbidden ? Show "unauthorized" message
    ??? 404 Not Found ? Return null
    ??? 401 Unauthorized ? Logout
```

---

## ? Final Verification

### Passenger App Changes Made

1. ? **AdminApi.cs** - Updated `GetDriverLocationAsync` to use passenger endpoint
2. ? **DriverTrackingService.cs** - Already using passenger endpoint (no change needed)
3. ? **AuthHttpHandler** - Already attaching JWT token (no change needed)
4. ? **Models** - Already have `PassengerLocationResponse` (no change needed)

### Build Status

```
Build successful ?
No compilation errors ?
All references resolved ?
```

### Ready for Testing

- [x] Code aligned with AdminAPI
- [x] Authorization flow complete
- [x] Error handling implemented
- [x] Models match API contract
- [x] Build successful
- [ ] **Ready for end-to-end testing**

---

## ?? Next Steps

### 1. Backend Deployment
Ensure AdminAPI has:
- ? `/passenger/rides/{rideId}/location` endpoint
- ? Email-based authorization
- ? `PassengerLocationResponse` format

### 2. AuthServer Verification
Verify JWT tokens contain:
- ? `email` claim
- ? Matches user's login email

### 3. Test Data Setup
Create test bookings where:
- Alice's booking has `booker.email = alice@example.com`
- Bob's booking has `booker.email = bob@example.com`

### 4. End-to-End Testing
Run test scenarios:
1. ? TC1: Passenger tracks own ride
2. ? TC2: Tracking not started
3. ? TC3: Unauthorized access
4. ? TC4: Token missing/expired

### 5. Monitor Logs
Watch for:
```
[AuthHttpHandler] Token retrieved for /passenger/rides/.../location: Present
[DriverTrackingService] Location received: 41.878100, -87.629800
[DriverTrackingService] State changed to: Tracking
```

---

## ?? Support

**Debug Checklist:**
1. ? Check JWT token has email claim (https://jwt.io)
2. ? Check email matches booking in database
3. ? Check driver status is OnRoute/Dispatched
4. ? Check driver location updates in AdminAPI logs
5. ? Check passenger app console logs

**Common Issues:**
- 403 Forbidden ? Email doesn't match booking
- 401 Unauthorized ? Token expired/missing
- "Not started" ? Driver hasn't started ride yet
- No location ? Driver not sending updates

---

**Date:** December 2024  
**Version:** 2.0.1  
**Status:** ? ALIGNED AND READY FOR TESTING  
**Changes:** AdminApi.cs updated to use passenger endpoint  
**Build:** ? Successful
