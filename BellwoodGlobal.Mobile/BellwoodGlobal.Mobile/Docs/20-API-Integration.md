# API Integration

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides complete reference for all AdminAPI endpoints consumed by the Bellwood Global Mobile App. It covers authentication, request/response formats, error handling, and usage examples.

**Base URL**: 
- **Production**: `https://api.bellwood.com`
- **Development**: `https://localhost:5206`

**Authentication**: JWT Bearer tokens (all endpoints require authentication unless noted)

**Serialization**: JSON (`application/json`)

---

## ?? Authentication

### Token Endpoint

**URL**: `POST {AuthServerUrl}/connect/token`

**Content-Type**: `application/x-www-form-urlencoded`

**Request Body**:
```
grant_type=password
&username={email}
&password={password}
&client_id=mobile-app
&scope=openid profile email admin-api
```

**Response** (200 OK):
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "refresh_token": "def50200..."
}
```

**Error Response** (400 Bad Request):
```json
{
  "error": "invalid_grant",
  "error_description": "Invalid username or password"
}
```

**Usage in Mobile App**:
```csharp
// Store token securely
await SecureStorage.SetAsync("access_token", response.AccessToken);

// Include in API requests
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);
```

---

## ?? Quote Endpoints

### POST /quotes - Submit Quote Request

**Purpose**: Submit a new quote request

**Authorization**: Required (role: `booker`)

**Request Body**:
```json
{
  "booker": {
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "312-555-0001",
    "emailAddress": "john.doe@example.com"
  },
  "passenger": {
    "firstName": "Jane",
    "lastName": "Smith",
    "phoneNumber": "312-555-0100",
    "emailAddress": "jane.smith@example.com"
  },
  "vehicleClass": "Sedan",
  "pickupDateTime": "2026-02-15T14:30:00Z",
  "pickupLocation": "O'Hare Airport Terminal 1",
  "pickupLatitude": 41.9742,
  "pickupLongitude": -87.9073,
  "pickupStyle": "Curbside",
  "dropoffLocation": "Downtown Chicago, 100 N LaSalle St",
  "dropoffLatitude": 41.8843,
  "dropoffLongitude": -87.6324,
  "roundTrip": false,
  "passengerCount": 2,
  "checkedBags": 2,
  "carryOnBags": 1,
  "additionalRequest": "None"
}
```

**Response** (200 OK):
```json
{
  "id": "quote-abc-123",
  "status": "Pending",
  "createdUtc": "2026-01-27T10:30:00Z"
}
```

**Error Responses**:
- **400 Bad Request**: Validation failed (e.g., missing required fields)
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: User doesn't have `booker` role

**Mobile App Usage**:
```csharp
var quoteDraft = new QuoteDraft
{
    Booker = new ContactInfo { ... },
    Passenger = new ContactInfo { ... },
    VehicleClass = "Sedan",
    PickupDateTime = pickupDateTime,
    PickupLocation = "O'Hare Airport Terminal 1",
    PickupLatitude = 41.9742,
    PickupLongitude = -87.9073,
    // ... other fields
};

var response = await _adminApi.SubmitQuoteAsync(quoteDraft);
// Navigate to dashboard, show success message
```

---

### GET /quotes/list - List User's Quotes

**Purpose**: Retrieve all quotes for authenticated user

**Authorization**: Required (user sees only their own quotes)

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `take` | int | No | 50 | Maximum number of results |
| `skip` | int | No | 0 | Number of results to skip (pagination) |

**Request**:
```http
GET /quotes/list?take=100 HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
[
  {
    "id": "quote-abc-123",
    "status": "Pending",
    "passengerName": "Jane Smith",
    "bookerName": "John Doe",
    "vehicleClass": "Sedan",
    "pickupDateTime": "2026-02-15T14:30:00Z",
    "pickupLocation": "O'Hare Airport Terminal 1",
    "dropoffLocation": "Downtown Chicago",
    "createdUtc": "2026-01-27T10:30:00Z",
    "estimatedPrice": null,
    "estimatedPickupTime": null,
    "respondedAt": null,
    "acknowledgedAt": null
  },
  {
    "id": "quote-def-456",
    "status": "Responded",
    "passengerName": "Alice Williams",
    "bookerName": "John Doe",
    "vehicleClass": "SUV",
    "pickupDateTime": "2026-02-20T09:00:00Z",
    "pickupLocation": "Midway Airport",
    "dropoffLocation": "Oak Park",
    "createdUtc": "2026-01-26T15:00:00Z",
    "estimatedPrice": 95.50,
    "estimatedPickupTime": "2026-02-20T08:45:00Z",
    "respondedAt": "2026-01-27T08:00:00Z",
    "acknowledgedAt": "2026-01-26T16:00:00Z"
  }
]
```

**Error Responses**:
- **401 Unauthorized**: Missing or invalid token

**Mobile App Usage**:
```csharp
// Get quotes for dashboard
var quotes = await _adminApi.GetQuotesAsync(take: 100);

// Filter by status (client-side)
var pendingQuotes = quotes.Where(q => q.Status == "Pending");

// Display in CollectionView
QuotesList.ItemsSource = quotes;
```

---

### GET /quotes/{id} - Get Quote Details

**Purpose**: Retrieve full details for a specific quote

**Authorization**: Required (user can only access their own quotes)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Quote ID (e.g., `quote-abc-123`) |

**Request**:
```http
GET /quotes/quote-abc-123 HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
{
  "id": "quote-abc-123",
  "status": "Responded",
  "passengerName": "Jane Smith",
  "bookerName": "John Doe",
  "vehicleClass": "Sedan",
  "pickupDateTime": "2026-02-15T14:30:00Z",
  "pickupLocation": "O'Hare Airport Terminal 1",
  "dropoffLocation": "Downtown Chicago",
  "createdUtc": "2026-01-27T10:30:00Z",
  "estimatedPrice": 85.50,
  "estimatedPickupTime": "2026-02-15T14:15:00Z",
  "notes": "Estimated based on standard route. Final price subject to confirmation.",
  "acknowledgedAt": "2026-01-27T11:00:00Z",
  "respondedAt": "2026-01-27T14:00:00Z",
  "draft": {
    "booker": {
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "312-555-0001",
      "emailAddress": "john.doe@example.com"
    },
    "passenger": {
      "firstName": "Jane",
      "lastName": "Smith",
      "phoneNumber": "312-555-0100",
      "emailAddress": "jane.smith@example.com"
    },
    "vehicleClass": "Sedan",
    "pickupDateTime": "2026-02-15T14:30:00Z",
    "pickupLocation": "O'Hare Airport Terminal 1",
    "pickupStyle": "Curbside",
    "dropoffLocation": "Downtown Chicago",
    "roundTrip": false,
    "passengerCount": 2,
    "checkedBags": 2,
    "carryOnBags": 1,
    "additionalRequest": "None"
  }
}
```

**Error Responses**:
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: Quote doesn't belong to user
- **404 Not Found**: Quote ID doesn't exist

**Mobile App Usage**:
```csharp
// Navigate to detail page with quote ID
await Shell.Current.GoToAsync($"QuoteDetailPage?id={quoteId}");

// In QuoteDetailPage
public async void ApplyQueryAttributes(IDictionary<string, object> query)
{
    var quoteId = query["id"].ToString();
    var quoteDetail = await _adminApi.GetQuoteAsync(quoteId);
    
    // Bind to UI
    BindQuoteDetails(quoteDetail);
}
```

---

### POST /quotes/{id}/accept - Accept Quote

**Purpose**: Accept a quote and create a booking (Phase Alpha)

**Authorization**: Required (user can only accept their own quotes)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Quote ID to accept |

**Request**:
```http
POST /quotes/quote-abc-123/accept HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
Content-Length: 0
```

**Response** (200 OK):
```json
{
  "bookingId": "booking-xyz-789",
  "message": "Quote accepted successfully. Booking created."
}
```

**Error Responses**:
- **400 Bad Request**: Quote not in "Responded" status (e.g., still "Pending")
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: Quote doesn't belong to user
- **404 Not Found**: Quote ID doesn't exist

**Mobile App Usage**:
```csharp
private async void OnAcceptQuoteClicked(object? sender, EventArgs e)
{
    try
    {
        var result = await _adminApi.AcceptQuoteAsync(quoteId);
        
        var navigateToBooking = await DisplayAlert(
            "Success!",
            "Quote accepted! Your booking has been created.",
            "View Booking",
            "OK");
        
        if (navigateToBooking)
        {
            await Shell.Current.GoToAsync($"BookingDetailPage?id={result.BookingId}");
        }
    }
    catch (InvalidOperationException ex)
    {
        // Business rule violation (e.g., wrong status)
        await DisplayAlert("Cannot Accept Quote", ex.Message, "OK");
    }
}
```

---

### POST /quotes/{id}/cancel - Cancel Quote

**Purpose**: Cancel a quote request

**Authorization**: Required (user can only cancel their own quotes)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Quote ID to cancel |

**Request**:
```http
POST /quotes/quote-abc-123/cancel HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
Content-Length: 0
```

**Response** (200 OK):
```json
{
  "message": "Quote cancelled successfully."
}
```

**Error Responses**:
- **400 Bad Request**: Quote already in terminal status ("Accepted" or "Cancelled")
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: Quote doesn't belong to user
- **404 Not Found**: Quote ID doesn't exist

**Mobile App Usage**:
```csharp
private async void OnCancelQuoteClicked(object? sender, EventArgs e)
{
    var confirm = await DisplayAlert(
        "Cancel Quote?",
        "Are you sure you want to cancel this quote request?",
        "Yes, Cancel",
        "No");
    
    if (!confirm) return;
    
    try
    {
        await _adminApi.CancelQuoteAsync(quoteId);
        await DisplayAlert("Quote Cancelled", "Your quote request has been cancelled.", "OK");
        await Shell.Current.GoToAsync("..");
    }
    catch (InvalidOperationException ex)
    {
        await DisplayAlert("Cannot Cancel Quote", ex.Message, "OK");
    }
}
```

---

## ?? Booking Endpoints

### POST /bookings - Submit Booking Request

**Purpose**: Submit a new booking (direct booking, not from quote)

**Authorization**: Required (role: `booker`)

**Request Body**: Same as `/quotes` request body

**Response** (200 OK):
```json
{
  "id": "booking-xyz-789",
  "status": "Requested",
  "createdUtc": "2026-01-27T10:30:00Z"
}
```

**Mobile App Usage**:
```csharp
var bookingRequest = new QuoteDraft { ... };
var response = await _adminApi.SubmitBookingAsync(bookingRequest);
await Shell.Current.GoToAsync($"BookingDetailPage?id={response.Id}");
```

---

### GET /bookings/list - List User's Bookings

**Purpose**: Retrieve all bookings for authenticated user

**Authorization**: Required (user sees only their own bookings)

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `take` | int | No | 50 | Maximum number of results |
| `skip` | int | No | 0 | Number of results to skip |

**Request**:
```http
GET /bookings/list?take=100 HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
[
  {
    "id": "booking-xyz-789",
    "rideId": "ride-123",
    "status": "Confirmed",
    "passengerName": "Jane Smith",
    "bookerName": "John Doe",
    "vehicleClass": "Sedan",
    "pickupDateTime": "2026-02-15T14:30:00Z",
    "pickupLocation": "O'Hare Airport Terminal 1",
    "dropoffLocation": "Downtown Chicago",
    "createdUtc": "2026-01-27T10:30:00Z",
    "confirmedPrice": 85.50
  }
]
```

**Mobile App Usage**:
```csharp
var bookings = await _adminApi.GetBookingsAsync(take: 100);
BookingsList.ItemsSource = bookings;
```

---

### GET /bookings/{id} - Get Booking Details

**Purpose**: Retrieve full details for a specific booking

**Authorization**: Required (user can only access their own bookings)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string | Booking ID |

**Request**:
```http
GET /bookings/booking-xyz-789 HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
{
  "id": "booking-xyz-789",
  "rideId": "ride-123",
  "status": "Confirmed",
  "passengerName": "Jane Smith",
  "bookerName": "John Doe",
  "vehicleClass": "Sedan",
  "pickupDateTime": "2026-02-15T14:30:00Z",
  "pickupLocation": "O'Hare Airport Terminal 1",
  "dropoffLocation": "Downtown Chicago",
  "createdUtc": "2026-01-27T10:30:00Z",
  "confirmedPrice": 85.50,
  "draft": { ... }
}
```

---

### POST /bookings/{id}/cancel - Cancel Booking

**Purpose**: Cancel a booking

**Authorization**: Required (user can only cancel their own bookings)

**Request**:
```http
POST /bookings/booking-xyz-789/cancel HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
{
  "message": "Booking cancelled successfully."
}
```

**Error Responses**:
- **400 Bad Request**: Booking cannot be cancelled (e.g., ride in progress)

---

## ?? Driver Tracking Endpoints

### GET /passenger/rides/{rideId}/location - Get Driver Location

**Purpose**: Get current GPS location of driver

**Authorization**: Required (email-based: passenger email must match booking)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `rideId` | string | Ride ID (from booking) |

**Request**:
```http
GET /passenger/rides/ride-123/location HTTP/1.1
Host: api.bellwood.com
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
{
  "rideId": "ride-123",
  "latitude": 41.8781,
  "longitude": -87.6298,
  "heading": 45.0,
  "speed": 35.5,
  "accuracy": 10.0,
  "timestamp": "2026-02-15T14:25:00Z"
}
```

**Response** (404 Not Found) - Tracking Not Started:
```json
{
  "error": "Tracking not started",
  "message": "Driver has not started tracking yet."
}
```

**Error Responses**:
- **401 Unauthorized**: Missing or invalid token
- **403 Forbidden**: User's email doesn't match booking passenger email
- **404 Not Found**: Ride ID doesn't exist OR tracking not started

**Mobile App Usage**:
```csharp
// Poll every 15 seconds for driver location
_pollingTimer = new Timer(15000);
_pollingTimer.Elapsed += async (s, e) =>
{
    try
    {
        var location = await _adminApi.GetDriverLocationAsync(rideId);
        
        if (location != null)
        {
            UpdateMapMarker(location.Latitude, location.Longitude);
            UpdateETA(location);
        }
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        // Tracking not started yet, show message
        TrackingMessage.Text = "Driver hasn't started tracking yet";
    }
};
_pollingTimer.Start();
```

---

## ?? HTTP Status Codes

### Success Codes

| Code | Meaning | Usage |
|------|---------|-------|
| **200 OK** | Request successful | GET requests, successful POST operations |
| **201 Created** | Resource created | POST /quotes, POST /bookings |

---

### Client Error Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **400 Bad Request** | Validation failed | Missing fields, invalid data, business rule violation |
| **401 Unauthorized** | Authentication required | Missing token, expired token, invalid token |
| **403 Forbidden** | Insufficient permissions | Wrong role, accessing other user's data |
| **404 Not Found** | Resource doesn't exist | Invalid ID, tracking not started |

---

### Server Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| **500 Internal Server Error** | Server error | Retry after delay, contact support |
| **503 Service Unavailable** | Server temporarily unavailable | Retry after delay |

---

## ??? Error Handling

### Standard Error Response Format

```json
{
  "error": "ValidationFailed",
  "message": "Pickup location is required",
  "details": {
    "field": "PickupLocation",
    "constraint": "NotEmpty"
  }
}
```

---

### Mobile App Error Handling Pattern

```csharp
try
{
    var result = await _adminApi.SomeOperationAsync();
    // Handle success
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
{
    // Validation error or business rule violation
    await DisplayAlert("Invalid Request", ex.Message, "OK");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    // Token expired, redirect to login
    await Shell.Current.GoToAsync("//LoginPage");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
{
    // Insufficient permissions
    await DisplayAlert("Access Denied", "You don't have permission for this action.", "OK");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Resource not found
    await DisplayAlert("Not Found", "The requested item could not be found.", "OK");
}
catch (HttpRequestException ex)
{
    // Network error
    await DisplayAlert("Network Error", $"Unable to connect: {ex.Message}", "OK");
}
catch (Exception ex)
{
    // Generic error
    await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
}
```

---

## ? Performance & Best Practices

### Request Optimization

**1. Use Pagination**:
```csharp
// Don't fetch all quotes at once
var quotes = await _adminApi.GetQuotesAsync(take: 50); // Default

// For "load more" functionality
var moreQuotes = await _adminApi.GetQuotesAsync(take: 50, skip: 50);
```

**2. Cache Where Appropriate**:
```csharp
// Cache quote list for 30 seconds
private List<QuoteListItem>? _cachedQuotes;
private DateTime _cacheExpiry;

public async Task<List<QuoteListItem>> GetQuotesAsync()
{
    if (_cachedQuotes != null && DateTime.UtcNow < _cacheExpiry)
        return _cachedQuotes;
    
    _cachedQuotes = await _adminApi.GetQuotesAsync();
    _cacheExpiry = DateTime.UtcNow.AddSeconds(30);
    return _cachedQuotes;
}
```

**3. Cancel Pending Requests**:
```csharp
private CancellationTokenSource? _cts;

public async Task LoadQuotesAsync()
{
    // Cancel previous request
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    
    try
    {
        var quotes = await _adminApi.GetQuotesAsync(ct: _cts.Token);
        // Update UI
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelled
    }
}
```

---

### Retry Logic

```csharp
public async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException ex) when (i < maxRetries - 1 && IsTransient(ex))
        {
            // Wait with exponential backoff
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
    
    // Final attempt
    return await operation();
}

private bool IsTransient(HttpRequestException ex)
{
    return ex.StatusCode == HttpStatusCode.ServiceUnavailable ||
           ex.StatusCode == HttpStatusCode.RequestTimeout;
}
```

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[21-Data-Models.md](21-Data-Models.md)** - Request/response DTOs
- **[23-Security-Model.md](23-Security-Model.md)** - Authentication & authorization
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common API issues

---

**Last Updated**: January 27, 2026  
**Version**: 1.0  
**Status**: ? Production Ready
