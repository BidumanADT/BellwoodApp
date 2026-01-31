# Passenger App - Quote Lifecycle Integration Guide

**Document Type**: Integration Guide - Mobile Development  
**Last Updated**: January 27, 2026  
**Target Audience**: Passenger App (MAUI) Development Team  
**Status**: ? Ready for Alpha Implementation

---

## ?? Overview

This document provides complete integration guidance for implementing **Phase Alpha Quote Lifecycle** features in the Passenger App (MAUI). The AdminAPI backend is fully implemented and ready; this guide covers the API contracts, workflows, and UI requirements for passenger-facing quote management.

**What's New in Phase Alpha**:
- ? **View Quote Requests** - Passengers can see all their submitted quotes
- ? **Track Quote Status** - Real-time status updates (Pending ? Acknowledged ? Responded)
- ? **Accept Quotes** - Convert dispatcher responses to confirmed bookings
- ? **Cancel Quotes** - Cancel quotes before acceptance
- ? **Price Transparency** - See estimated price/ETA from dispatchers

---

## ?? Phase Alpha Objectives (Passenger App)

From the alpha test preparation plan (Section 3):

### Primary Goals
1. **Enable quote tracking** - Show passengers their quote requests and status
2. **Surface dispatcher responses** - Display estimated price/ETA when dispatchers respond
3. **Allow quote acceptance** - Convert responded quotes to bookings with one tap
4. **Support cancellation** - Let passengers cancel quotes they no longer need
5. **Provide status notifications** - Alert passengers when quote status changes

### Success Criteria
- ? Passengers can view all their quotes in one place
- ? Quote status changes are visible within 30 seconds (polling)
- ? Accepting a quote creates a booking and navigates to booking details
- ? All actions respect ownership (users only see/modify their own quotes)
- ? Clear messaging explains each status and next steps

---

## ?? API Integration Reference

### Base Configuration

```csharp
// App configuration (appsettings.json or Constants.cs)
public static class ApiConfig
{
    // Development
    public const string AdminApiBaseUrl = "https://localhost:5206";
    public const string AuthServerBaseUrl = "https://localhost:5001";
    
    // Production (update for deployment)
    // public const string AdminApiBaseUrl = "https://api.bellwood.com";
    // public const string AuthServerBaseUrl = "https://auth.bellwood.com";
}
```

### Authentication

All quote endpoints require JWT authentication:

```csharp
// Add JWT to HTTP client headers
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);
```

**JWT Claims Used**:
- `sub` - Username
- `uid` - User ID (used for ownership filtering)
- `email` - Email address (used for authorization)
- `role` - User role (typically "booker" for passengers)

---

## ?? API Endpoints for Passenger App

### 1. List User's Quotes

**Endpoint**: `GET /quotes/list?take=50`  
**Authorization**: Authenticated (booker)  
**Purpose**: Retrieve all quotes created by the logged-in passenger

**Request**:
```http
GET /quotes/list?take=50
Authorization: Bearer {jwt_token}
```

**Query Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `take` | int | 50 | Number of quotes to return (max 200) |

**Response** (200 OK):
```json
[
  {
    "id": "quote-abc-123",
    "createdUtc": "2026-01-27T10:30:00Z",
    "status": "Responded",
    "bookerName": "John Smith",
    "passengerName": "Jane Smith",
    "vehicleClass": "Sedan",
    "pickupLocation": "O'Hare International Airport",
    "dropoffLocation": "Downtown Chicago, 100 N LaSalle St",
    "pickupDateTime": "2026-01-30T15:00:00Z"
  },
  {
    "id": "quote-def-456",
    "createdUtc": "2026-01-26T14:20:00Z",
    "status": "Pending",
    "bookerName": "John Smith",
    "passengerName": "John Smith",
    "vehicleClass": "SUV",
    "pickupLocation": "Midway Airport",
    "dropoffLocation": "Oak Park, 150 N Oak Park Ave",
    "pickupDateTime": "2026-02-01T09:00:00Z"
  }
]
```

**Field Descriptions**:
| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique quote identifier |
| `createdUtc` | DateTime (UTC) | When quote was submitted |
| `status` | string | Current quote status (see status table below) |
| `bookerName` | string | Name of person who requested quote |
| `passengerName` | string | Name of passenger (may differ from booker) |
| `vehicleClass` | string | Requested vehicle type |
| `pickupLocation` | string | Pickup address |
| `dropoffLocation` | string | Destination address |
| `pickupDateTime` | DateTime (UTC) | Requested pickup time |

**Filtering Behavior**:
- API **automatically filters** by `CreatedByUserId` (from JWT `uid` claim)
- Passengers **only see their own quotes** (no manual filtering needed)
- Legacy quotes without `CreatedByUserId` are hidden from non-staff users

---

### 2. Get Quote Details

**Endpoint**: `GET /quotes/{id}`  
**Authorization**: Authenticated (quote owner or staff)  
**Purpose**: Retrieve full quote details including dispatcher response

**Request**:
```http
GET /quotes/quote-abc-123
Authorization: Bearer {jwt_token}
```

**Response** (200 OK):
```json
{
  "id": "quote-abc-123",
  "status": "Responded",
  "createdUtc": "2026-01-27T10:30:00Z",
  "bookerName": "John Smith",
  "passengerName": "Jane Smith",
  "vehicleClass": "Sedan",
  "pickupLocation": "O'Hare International Airport",
  "dropoffLocation": "Downtown Chicago, 100 N LaSalle St",
  "pickupDateTime": "2026-01-30T15:00:00Z",
  "draft": {
    "booker": {
      "firstName": "John",
      "lastName": "Smith",
      "phoneNumber": "312-555-1001",
      "emailAddress": "john.smith@example.com"
    },
    "passenger": {
      "firstName": "Jane",
      "lastName": "Smith",
      "phoneNumber": "312-555-1002",
      "emailAddress": "jane.smith@example.com"
    },
    "vehicleClass": "Sedan",
    "pickupDateTime": "2026-01-30T15:00:00",
    "pickupLocation": "O'Hare International Airport",
    "pickupStyle": "Curbside",
    "dropoffLocation": "Downtown Chicago, 100 N LaSalle St",
    "roundTrip": false,
    "passengerCount": 2,
    "checkedBags": 2,
    "carryOnBags": 1
  },
  
  "createdByUserId": "user-abc-123",
  "modifiedByUserId": "dispatcher-xyz",
  "modifiedOnUtc": "2026-01-27T14:15:00Z",
  
  "acknowledgedAt": "2026-01-27T11:00:00Z",
  "acknowledgedByUserId": "dispatcher-xyz",
  
  "respondedAt": "2026-01-27T14:15:00Z",
  "respondedByUserId": "dispatcher-xyz",
  "estimatedPrice": 85.50,
  "estimatedPickupTime": "2026-01-30T14:45:00Z",
  "notes": "Estimated based on standard route pricing. Final price subject to confirmation."
}
```

**Phase Alpha Lifecycle Fields**:
| Field | Type | Description |
|-------|------|-------------|
| `acknowledgedAt` | DateTime? (UTC) | When dispatcher acknowledged receipt |
| `acknowledgedByUserId` | string? | Dispatcher who acknowledged |
| `respondedAt` | DateTime? (UTC) | When dispatcher sent price/ETA |
| `respondedByUserId` | string? | Dispatcher who responded |
| `estimatedPrice` | decimal? | **?? Placeholder price** from dispatcher |
| `estimatedPickupTime` | DateTime? (UTC) | Estimated pickup time (may differ from requested) |
| `notes` | string? | Additional notes from dispatcher |

**Important Notes**:
- ?? `estimatedPrice` is a **placeholder** until Limo Anywhere integration (Phase 3+)
- Display prices with disclaimer: "Estimated price - subject to final confirmation"
- `null` values mean dispatcher hasn't reached that lifecycle stage yet

**Error Responses**:
```json
// 404 Not Found
{
  "error": "Quote not found"
}

// 403 Forbidden (not quote owner)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "detail": "You do not have permission to view this quote",
  "status": 403
}
```

---

### 3. Accept Quote

**Endpoint**: `POST /quotes/{id}/accept`  
**Authorization**: Authenticated (quote owner only)  
**Purpose**: Accept dispatcher's response and convert quote to booking

**Request**:
```http
POST /quotes/quote-abc-123/accept
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body**: None (empty body)

**Success Response** (200 OK):
```json
{
  "message": "Quote accepted and booking created successfully",
  "quoteId": "quote-abc-123",
  "quoteStatus": "Accepted",
  "bookingId": "booking-xyz-789",
  "bookingStatus": "Requested",
  "sourceQuoteId": "quote-abc-123"
}
```

**What Happens**:
1. ? Quote status changes to `Accepted` (terminal state)
2. ? New booking created with:
   - Status = `Requested` (awaiting dispatcher confirmation)
   - Pickup/dropoff data from quote
   - Estimated price from quote response
   - `SourceQuoteId` linking back to original quote
3. ? Email notification sent to Bellwood staff
4. ? Audit log created

**UI Flow**:
```csharp
// After successful acceptance:
1. Show success message: "Quote accepted! Booking created."
2. Navigate to booking detail page
3. Pass bookingId to BookingDetailPage
4. Remove quote from "active quotes" list (it's now accepted)
```

**Error Responses**:

```json
// 400 Bad Request - Wrong status
{
  "error": "Can only accept quotes with status 'Responded'. Current status: Pending"
}

// 403 Forbidden - Not quote owner
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "detail": "Only the booker who requested this quote can accept it",
  "status": 403
}

// 404 Not Found
{
  "error": "Quote not found"
}
```

**Security Note**:
- ? **Staff (admin/dispatcher) CANNOT accept quotes on behalf of passengers**
- ? Only the original booker (`CreatedByUserId` match) can accept
- This prevents fraudulent quote acceptance

---

### 4. Cancel Quote

**Endpoint**: `POST /quotes/{id}/cancel`  
**Authorization**: Authenticated (quote owner or staff)  
**Purpose**: Cancel a quote request

**Request**:
```http
POST /quotes/quote-abc-123/cancel
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body**: None (empty body)

**Success Response** (200 OK):
```json
{
  "message": "Quote cancelled successfully",
  "id": "quote-abc-123",
  "status": "Cancelled"
}
```

**What Happens**:
1. ? Quote status changes to `Cancelled` (terminal state)
2. ? `ModifiedByUserId` and `ModifiedOnUtc` updated
3. ? Audit log created

**Error Responses**:

```json
// 400 Bad Request - Cannot cancel terminal status
{
  "error": "Cannot cancel quote with status 'Accepted'"
}

// 403 Forbidden - Not authorized
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "detail": "You do not have permission to cancel this quote",
  "status": 403
}
```

**Allowed Cancellations**:
| Current Status | Can Cancel? |
|----------------|-------------|
| `Pending` | ? Yes |
| `Acknowledged` | ? Yes |
| `Responded` | ? Yes |
| `Accepted` | ? No (use booking cancellation instead) |
| `Cancelled` | ? Already cancelled |

---

## ?? Quote Status Reference

### Status Values & Meanings

| Status | Passenger View | Description | Actions Available |
|--------|----------------|-------------|-------------------|
| **Pending** | "Awaiting Response" | Quote submitted, dispatcher hasn't acknowledged yet | Cancel |
| **Acknowledged** | "Under Review" | Dispatcher acknowledged receipt, preparing price/ETA | Cancel |
| **Responded** | "Response Received" | Dispatcher sent estimated price/ETA | Accept, Cancel |
| **Accepted** | "Booking Created" | Passenger accepted, booking created | View Booking |
| **Cancelled** | "Cancelled" | Quote cancelled by passenger or staff | None (read-only) |

### Status Lifecycle Flow

```
???????????????????????????????????????????????????????????????
?                    PASSENGER SUBMITS QUOTE                  ?
?                                                             ?
?  POST /quotes                                               ?
?  ?                                                          ?
?  Status: PENDING                                            ?
?  Message: "Your quote request has been submitted"          ?
???????????????????????????????????????????????????????????????
                          ?
???????????????????????????????????????????????????????????????
?              DISPATCHER ACKNOWLEDGES (AdminPortal)          ?
?                                                             ?
?  POST /quotes/{id}/acknowledge (StaffOnly)                  ?
?  ?                                                          ?
?  Status: ACKNOWLEDGED                                       ?
?  Message: "Your quote is being reviewed"                   ?
???????????????????????????????????????????????????????????????
                          ?
???????????????????????????????????????????????????????????????
?           DISPATCHER SENDS PRICE/ETA (AdminPortal)          ?
?                                                             ?
?  POST /quotes/{id}/respond (StaffOnly)                      ?
?  Payload: { estimatedPrice, estimatedPickupTime, notes }   ?
?  ?                                                          ?
?  Status: RESPONDED                                          ?
?  Message: "We've sent you a quote!"                        ?
?  ?? Email notification sent to passenger                   ?
???????????????????????????????????????????????????????????????
                          ?
         ???????????????????????????????????
         ?                                  ?
         ?                                  ?
????????????????????????       ????????????????????????
? PASSENGER ACCEPTS    ?       ? PASSENGER CANCELS    ?
?                      ?       ?                      ?
? POST /accept         ?       ? POST /cancel         ?
? ?                    ?       ? ?                    ?
? Status: ACCEPTED     ?       ? Status: CANCELLED    ?
? ?? Booking Created!  ?       ? ? Quote Cancelled    ?
????????????????????????       ????????????????????????
```

---

## ?? UI/UX Implementation Guide

### 1. "My Quotes" Page

**Purpose**: List all quotes submitted by the passenger

**Navigation**: 
- Add to main menu: "My Quotes" or "Quote Requests"
- Icon suggestion: ?? or ??

**Layout**:

```
???????????????????????????????????????????????????????
?  My Quotes                                     [+]  ? ? New Quote button
???????????????????????????????????????????????????????
?                                                     ?
?  ?? O'Hare Airport ? Downtown                      ?
?  ???  Jan 30, 3:00 PM                               ?
?  ?? Sedan                                           ?
?  ? Response Received - $85.50                     ? ? Status badge
?                                            [View >] ?
?                                                     ?
???????????????????????????????????????????????????????
?                                                     ?
?  ?? Midway ? Oak Park                              ?
?  ???  Feb 1, 9:00 AM                                ?
?  ?? SUV                                             ?
?  ? Awaiting Response                              ? ? Status badge
?                                            [View >] ?
?                                                     ?
???????????????????????????????????????????????????????
?                                                     ?
?  ?? Union Station ? O'Hare                         ?
?  ???  Feb 5, 6:00 PM                                ?
?  ?? S-Class                                         ?
?  ? Cancelled                                       ? ? Grayed out
?                                            [View >] ?
?                                                     ?
???????????????????????????????????????????????????????
```

**Status Badge Colors**:
```csharp
// Suggested color scheme
var statusColors = new Dictionary<string, Color>
{
    ["Pending"] = Colors.Orange,       // ? Awaiting Response
    ["Acknowledged"] = Colors.Blue,    // ?? Under Review
    ["Responded"] = Colors.Green,      // ? Response Received
    ["Accepted"] = Colors.Gray,        // ? Booking Created
    ["Cancelled"] = Colors.Red         // ? Cancelled
};
```

**Data Refresh**:
```csharp
// Polling strategy (until WebSockets implemented)
public class MyQuotesViewModel
{
    private System.Timers.Timer _pollingTimer;
    
    public void StartPolling()
    {
        // Poll every 30 seconds
        _pollingTimer = new System.Timers.Timer(30_000);
        _pollingTimer.Elapsed += async (s, e) => await RefreshQuotesAsync();
        _pollingTimer.Start();
        
        // Initial load
        await RefreshQuotesAsync();
    }
    
    private async Task RefreshQuotesAsync()
    {
        try
        {
            var quotes = await _quoteService.GetMyQuotesAsync();
            
            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Quotes.Clear();
                foreach (var quote in quotes)
                {
                    Quotes.Add(quote);
                }
            });
        }
        catch (Exception ex)
        {
            // Log error but don't interrupt user
            Debug.WriteLine($"Quote refresh failed: {ex.Message}");
        }
    }
    
    public void StopPolling()
    {
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
    }
}
```

---

### 2. Quote Detail Page

**Purpose**: Show full quote details and allow actions based on status

**Layout**:

```
???????????????????????????????????????????????????????
?  ? Back          Quote Details                      ?
???????????????????????????????????????????????????????
?                                                     ?
?  Status: ? Response Received                      ? ? Large status badge
?                                                     ?
?  ?????????????????????????????????????????????  ?
?                                                     ?
?  ?? TRIP DETAILS                                    ?
?                                                     ?
?  From:     O'Hare International Airport            ?
?  To:       Downtown Chicago, 100 N LaSalle St      ?
?  When:     January 30, 2026 @ 3:00 PM              ?
?  Vehicle:  Sedan                                    ?
?  Passengers: 2 | Bags: 2 checked, 1 carry-on       ?
?                                                     ?
?  ?????????????????????????????????????????????  ?
?                                                     ?
?  ?? DISPATCHER RESPONSE                            ?
?                                                     ?
?  Estimated Price:  $85.50                          ?
?  ?? Placeholder price - final cost may vary        ? ? Disclaimer
?                                                     ?
?  Pickup Time:      2:45 PM (15 min early)          ?
?  Notes:            "Estimated based on standard    ?
?                    route pricing. Final price      ?
?                    subject to confirmation."       ?
?                                                     ?
?  ?????????????????????????????????????????????  ?
?                                                     ?
?  ?? PASSENGER CONTACT                              ?
?                                                     ?
?  Name:     Jane Smith                              ?
?  Phone:    312-555-1002                            ?
?  Email:    jane.smith@example.com                  ?
?                                                     ?
?  ?????????????????????????????????????????????  ?
?                                                     ?
?  [   Accept Quote & Create Booking   ]             ? ? Primary action
?  [        Cancel Quote Request        ]             ? ? Secondary action
?                                                     ?
???????????????????????????????????????????????????????
```

**Dynamic UI Based on Status**:

```csharp
public partial class QuoteDetailPage : ContentPage
{
    private void UpdateUIForStatus(QuoteDetailDto quote)
    {
        switch (quote.Status)
        {
            case "Pending":
                StatusLabel.Text = "? Awaiting Response";
                StatusLabel.BackgroundColor = Colors.Orange;
                MessageLabel.Text = "Your quote request has been submitted. A dispatcher will review it shortly.";
                
                // Hide response section
                ResponseSection.IsVisible = false;
                
                // Show cancel button only
                AcceptButton.IsVisible = false;
                CancelButton.IsVisible = true;
                break;
                
            case "Acknowledged":
                StatusLabel.Text = "?? Under Review";
                StatusLabel.BackgroundColor = Colors.Blue;
                MessageLabel.Text = "Your quote is being reviewed by our dispatch team.";
                
                // Hide response section
                ResponseSection.IsVisible = false;
                
                // Show cancel button only
                AcceptButton.IsVisible = false;
                CancelButton.IsVisible = true;
                break;
                
            case "Responded":
                StatusLabel.Text = "? Response Received";
                StatusLabel.BackgroundColor = Colors.Green;
                MessageLabel.Text = "We've prepared an estimate for your trip!";
                
                // Show response section
                ResponseSection.IsVisible = true;
                EstimatedPriceLabel.Text = $"${quote.EstimatedPrice:F2}";
                EstimatedPickupLabel.Text = quote.EstimatedPickupTime?.ToString("MMM dd, yyyy @ h:mm tt");
                NotesLabel.Text = quote.Notes ?? "No additional notes";
                
                // Show both buttons
                AcceptButton.IsVisible = true;
                CancelButton.IsVisible = true;
                break;
                
            case "Accepted":
                StatusLabel.Text = "? Booking Created";
                StatusLabel.BackgroundColor = Colors.Gray;
                MessageLabel.Text = "This quote has been accepted. Your booking is ready!";
                
                // Show response section (read-only)
                ResponseSection.IsVisible = true;
                
                // Hide action buttons, show "View Booking" instead
                AcceptButton.IsVisible = false;
                CancelButton.IsVisible = false;
                ViewBookingButton.IsVisible = true;
                ViewBookingButton.CommandParameter = quote.BookingId; // If available
                break;
                
            case "Cancelled":
                StatusLabel.Text = "? Cancelled";
                StatusLabel.BackgroundColor = Colors.Red;
                MessageLabel.Text = "This quote request was cancelled.";
                
                // Hide everything - read-only
                ResponseSection.IsVisible = false;
                AcceptButton.IsVisible = false;
                CancelButton.IsVisible = false;
                break;
        }
    }
}
```

---

### 3. Accept Quote Flow

**User Action**: Tap "Accept Quote & Create Booking"

**Implementation**:

```csharp
private async Task AcceptQuoteAsync()
{
    // Show loading indicator
    IsLoading = true;
    
    try
    {
        // Call API
        var result = await _quoteService.AcceptQuoteAsync(QuoteId);
        
        // Show success message
        await DisplayAlert(
            "Success!", 
            "Quote accepted! Your booking has been created.", 
            "View Booking");
        
        // Navigate to booking detail page
        await Shell.Current.GoToAsync($"booking-detail?id={result.BookingId}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
        // Wrong status (e.g., already accepted or not responded yet)
        await DisplayAlert(
            "Cannot Accept Quote", 
            "This quote cannot be accepted at this time. Please check its status.", 
            "OK");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        // Not authorized (shouldn't happen for own quotes)
        await DisplayAlert(
            "Access Denied", 
            "You don't have permission to accept this quote.", 
            "OK");
    }
    catch (Exception ex)
    {
        // Generic error
        await DisplayAlert(
            "Error", 
            $"Failed to accept quote: {ex.Message}", 
            "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

### 4. Cancel Quote Flow

**User Action**: Tap "Cancel Quote Request"

**Implementation**:

```csharp
private async Task CancelQuoteAsync()
{
    // Confirm cancellation
    var confirm = await DisplayAlert(
        "Cancel Quote?", 
        "Are you sure you want to cancel this quote request? This action cannot be undone.", 
        "Yes, Cancel", 
        "No");
    
    if (!confirm)
        return;
    
    // Show loading indicator
    IsLoading = true;
    
    try
    {
        // Call API
        await _quoteService.CancelQuoteAsync(QuoteId);
        
        // Show success message
        await DisplayAlert(
            "Quote Cancelled", 
            "Your quote request has been cancelled.", 
            "OK");
        
        // Go back to quotes list
        await Shell.Current.GoToAsync("..");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
        // Cannot cancel (e.g., already accepted)
        await DisplayAlert(
            "Cannot Cancel", 
            "This quote cannot be cancelled. It may have already been accepted or converted to a booking.", 
            "OK");
    }
    catch (Exception ex)
    {
        // Generic error
        await DisplayAlert(
            "Error", 
            $"Failed to cancel quote: {ex.Message}", 
            "OK");
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

### 5. Notifications (Future Enhancement)

**Alpha Test Approach**: Polling (30-second intervals)

**Phase 3+ Approach**: Push notifications

```csharp
// Placeholder for future push notification handler
public class QuoteNotificationHandler
{
    public void OnQuoteStatusChanged(string quoteId, string newStatus)
    {
        var notification = new NotificationRequest
        {
            Title = "Quote Update",
            Description = GetNotificationMessage(newStatus),
            BadgeNumber = GetPendingQuoteCount(),
            CategoryType = "quote_update"
        };
        
        LocalNotificationCenter.Current.Show(notification);
    }
    
    private string GetNotificationMessage(string status)
    {
        return status switch
        {
            "Acknowledged" => "Your quote is being reviewed!",
            "Responded" => "We've sent you a price estimate!",
            "Accepted" => "Your booking has been created!",
            "Cancelled" => "Your quote was cancelled",
            _ => "Quote status updated"
        };
    }
}
```

---

## ?? Service Layer Implementation

### QuoteService.cs

```csharp
public interface IQuoteService
{
    Task<List<QuoteListItemDto>> GetMyQuotesAsync(int take = 50);
    Task<QuoteDetailDto> GetQuoteDetailsAsync(string quoteId);
    Task<AcceptQuoteResponseDto> AcceptQuoteAsync(string quoteId);
    Task CancelQuoteAsync(string quoteId);
}

public class QuoteService : IQuoteService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    
    public QuoteService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }
    
    public async Task<List<QuoteListItemDto>> GetMyQuotesAsync(int take = 50)
    {
        // Ensure authenticated
        await EnsureAuthenticatedAsync();
        
        var response = await _httpClient.GetAsync($"/quotes/list?take={take}");
        response.EnsureSuccessStatusCode();
        
        var quotes = await response.Content.ReadFromJsonAsync<List<QuoteListItemDto>>();
        return quotes ?? new List<QuoteListItemDto>();
    }
    
    public async Task<QuoteDetailDto> GetQuoteDetailsAsync(string quoteId)
    {
        await EnsureAuthenticatedAsync();
        
        var response = await _httpClient.GetAsync($"/quotes/{quoteId}");
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("Quote not found");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("You don't have permission to view this quote");
        
        response.EnsureSuccessStatusCode();
        
        var quote = await response.Content.ReadFromJsonAsync<QuoteDetailDto>();
        return quote ?? throw new InvalidOperationException("Failed to deserialize quote");
    }
    
    public async Task<AcceptQuoteResponseDto> AcceptQuoteAsync(string quoteId)
    {
        await EnsureAuthenticatedAsync();
        
        var response = await _httpClient.PostAsync($"/quotes/{quoteId}/accept", null);
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Cannot accept quote");
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("You don't have permission to accept this quote");
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<AcceptQuoteResponseDto>();
        return result ?? throw new InvalidOperationException("Failed to accept quote");
    }
    
    public async Task CancelQuoteAsync(string quoteId)
    {
        await EnsureAuthenticatedAsync();
        
        var response = await _httpClient.PostAsync($"/quotes/{quoteId}/cancel", null);
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            throw new InvalidOperationException(error?.Error ?? "Cannot cancel quote");
        }
        
        response.EnsureSuccessStatusCode();
    }
    
    private async Task EnsureAuthenticatedAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new UnauthorizedAccessException("Not authenticated");
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
}
```

---

### DTOs (Data Transfer Objects)

```csharp
// Quote list item (for "My Quotes" page)
public class QuoteListItemDto
{
    public string Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string Status { get; set; }
    public string BookerName { get; set; }
    public string PassengerName { get; set; }
    public string VehicleClass { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
}

// Quote detail (for quote detail page)
public class QuoteDetailDto
{
    public string Id { get; set; }
    public string Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string BookerName { get; set; }
    public string PassengerName { get; set; }
    public string VehicleClass { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public QuoteDraft Draft { get; set; } // Full draft data
    
    // Lifecycle fields
    public string CreatedByUserId { get; set; }
    public string ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string RespondedByUserId { get; set; }
    
    // Response fields
    public decimal? EstimatedPrice { get; set; }
    public DateTime? EstimatedPickupTime { get; set; }
    public string Notes { get; set; }
}

// Accept quote response
public class AcceptQuoteResponseDto
{
    public string Message { get; set; }
    public string QuoteId { get; set; }
    public string QuoteStatus { get; set; }
    public string BookingId { get; set; }
    public string BookingStatus { get; set; }
    public string SourceQuoteId { get; set; }
}

// Error response
public class ErrorResponse
{
    public string Error { get; set; }
}
```

---

## ?? Testing Checklist

### Manual Testing Scenarios

**Scenario 1: View My Quotes**
- [ ] Navigate to "My Quotes" page
- [ ] Verify quotes list displays
- [ ] Verify only user's own quotes appear (no other users' quotes)
- [ ] Verify status badges display correct colors
- [ ] Tap "Refresh" and verify list updates

**Scenario 2: View Quote Details (Pending)**
- [ ] Tap a Pending quote
- [ ] Verify status shows "? Awaiting Response"
- [ ] Verify dispatcher response section is hidden
- [ ] Verify only "Cancel" button is visible
- [ ] Verify trip details are accurate

**Scenario 3: View Quote Details (Responded)**
- [ ] Tap a Responded quote
- [ ] Verify status shows "? Response Received"
- [ ] Verify estimated price displays with disclaimer
- [ ] Verify estimated pickup time displays
- [ ] Verify dispatcher notes display
- [ ] Verify both "Accept" and "Cancel" buttons are visible

**Scenario 4: Accept Quote**
- [ ] Tap "Accept Quote & Create Booking"
- [ ] Verify success message appears
- [ ] Verify navigation to booking detail page
- [ ] Verify booking ID matches response
- [ ] Return to quotes list and verify status changed to "Accepted"

**Scenario 5: Cancel Quote**
- [ ] Tap "Cancel Quote Request"
- [ ] Verify confirmation dialog appears
- [ ] Tap "Yes, Cancel"
- [ ] Verify success message
- [ ] Verify quote removed from active list or status changed to "Cancelled"

**Scenario 6: Polling Updates**
- [ ] Open "My Quotes" page
- [ ] Have dispatcher respond to a quote (via AdminPortal)
- [ ] Wait 30 seconds
- [ ] Verify quote status updates automatically
- [ ] Verify price/ETA appears without manual refresh

**Scenario 7: Error Handling**
- [ ] Try accepting a Pending quote ? Verify error message
- [ ] Try cancelling an Accepted quote ? Verify error message
- [ ] Try viewing another user's quote ? Verify 403 Forbidden
- [ ] Disconnect network and try refresh ? Verify graceful error

---

### Integration Test Examples

```csharp
[Test]
public async Task GetMyQuotes_ReturnsOnlyOwnQuotes()
{
    // Arrange
    var service = CreateQuoteService(userToken: "user-123-token");
    
    // Act
    var quotes = await service.GetMyQuotesAsync();
    
    // Assert
    Assert.IsNotNull(quotes);
    Assert.IsTrue(quotes.All(q => q.CreatedByUserId == "user-123"));
}

[Test]
public async Task AcceptQuote_RespondedStatus_CreatesBooking()
{
    // Arrange
    var service = CreateQuoteService();
    var quoteId = "quote-with-responded-status";
    
    // Act
    var result = await service.AcceptQuoteAsync(quoteId);
    
    // Assert
    Assert.AreEqual("Accepted", result.QuoteStatus);
    Assert.IsNotNull(result.BookingId);
    Assert.AreEqual(quoteId, result.SourceQuoteId);
}

[Test]
public async Task AcceptQuote_PendingStatus_ThrowsException()
{
    // Arrange
    var service = CreateQuoteService();
    var quoteId = "quote-with-pending-status";
    
    // Act & Assert
    Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await service.AcceptQuoteAsync(quoteId);
    });
}

[Test]
public async Task CancelQuote_AcceptedStatus_ThrowsException()
{
    // Arrange
    var service = CreateQuoteService();
    var quoteId = "quote-with-accepted-status";
    
    // Act & Assert
    Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await service.CancelQuoteAsync(quoteId);
    });
}
```

---

## ?? Important Notes & Limitations

### Alpha Test Limitations

?? **Placeholder Pricing**
- Estimated prices are **manually entered** by dispatchers
- Not actual Limo Anywhere quotes (integration in Phase 3+)
- Display disclaimer: "Estimated price - subject to final confirmation"

?? **Polling Instead of WebSockets**
- Status updates via **30-second polling** (not real-time)
- Push notifications not implemented yet
- Acceptable for alpha test; WebSockets in Phase 3+

?? **No Refresh Tokens**
- JWTs expire and must be re-issued manually
- Users may need to re-login during long sessions
- Refresh token flow planned for Phase 3+

?? **JSON Storage**
- File-based storage may have concurrency issues
- Database migration planned for Phase 3+
- Acceptable for alpha test scale

### Security Notes

? **Ownership Enforcement**
- API automatically filters quotes by `CreatedByUserId`
- Passengers cannot see other users' quotes
- Passengers can only accept/cancel their own quotes

? **Staff Cannot Accept on Behalf of Passengers**
- Only the original booker can accept quotes
- Prevents fraudulent quote acceptance
- Staff can only acknowledge, respond, or cancel (for support)

---

## ?? Support & Troubleshooting

### Common Issues

**Issue 1: 403 Forbidden on quote endpoints**
- **Cause**: JWT missing or invalid
- **Fix**: Ensure `Authorization: Bearer {token}` header is set
- **Fix**: Check token hasn't expired

**Issue 2: Empty quotes list**
- **Cause**: User hasn't submitted any quotes yet
- **Fix**: Show empty state with "Submit your first quote" prompt
- **Cause**: API filtering by wrong user ID
- **Fix**: Verify JWT `uid` claim matches quote `CreatedByUserId`

**Issue 3: Cannot accept quote (400 Bad Request)**
- **Cause**: Quote not in `Responded` status yet
- **Fix**: Check quote status; only `Responded` quotes can be accepted
- **Cause**: Quote already accepted or cancelled
- **Fix**: Refresh quote details to get latest status

**Issue 4: Estimated price is null**
- **Cause**: Dispatcher hasn't responded yet
- **Fix**: Hide price section until status is `Responded`

---

### Contact & Resources

**API Documentation**: `Docs/15-Quote-Lifecycle.md`  
**API Reference**: `Docs/20-API-Reference.md`  
**Test Data Scripts**: `Scripts/Seed-Quotes.ps1`  

**Support Channels**:
- Backend Team: For API issues or bugs
- Design Team: For UI/UX questions
- QA Team: For test data or scenarios

---

## ? Implementation Checklist

### Phase Alpha - Quote Lifecycle (Passenger App)

**UI Components**:
- [ ] Create "My Quotes" list page
- [ ] Create Quote Detail page
- [ ] Add status badges with colors
- [ ] Add Accept/Cancel action buttons
- [ ] Add polling logic (30-second refresh)
- [ ] Add empty state for no quotes
- [ ] Add loading indicators

**Service Layer**:
- [ ] Implement `IQuoteService` interface
- [ ] Add `GetMyQuotesAsync()` method
- [ ] Add `GetQuoteDetailsAsync()` method
- [ ] Add `AcceptQuoteAsync()` method
- [ ] Add `CancelQuoteAsync()` method
- [ ] Add authentication handling
- [ ] Add error handling

**DTOs**:
- [ ] Create `QuoteListItemDto`
- [ ] Create `QuoteDetailDto`
- [ ] Create `AcceptQuoteResponseDto`
- [ ] Create `ErrorResponse`

**Navigation**:
- [ ] Add "My Quotes" to main menu
- [ ] Add route for quote detail page
- [ ] Add navigation from quote list to detail
- [ ] Add navigation from accepted quote to booking

**Testing**:
- [ ] Test quote list display
- [ ] Test quote detail display
- [ ] Test accept quote flow
- [ ] Test cancel quote flow
- [ ] Test status polling
- [ ] Test error scenarios
- [ ] Test with multiple users

**Documentation**:
- [ ] Update user guide
- [ ] Document new features in release notes
- [ ] Create alpha test instructions

---

## ?? Summary

The AdminAPI backend is **fully implemented and ready** for Phase Alpha quote lifecycle integration. This guide provides everything the Passenger App team needs to:

1. ? **View quotes** - List and detail views with proper filtering
2. ? **Track status** - Real-time updates via polling
3. ? **Accept quotes** - Convert dispatcher responses to bookings
4. ? **Cancel quotes** - Cancel unwanted quote requests
5. ? **Secure access** - Ownership-based authorization

**Key Implementation Points**:
- Use existing `QuoteDraft` model for quote submission (already implemented)
- Add new pages for quote list and detail views
- Implement polling for status updates (30-second intervals)
- Display placeholder pricing with disclaimers
- Navigate to booking detail after quote acceptance

**Alpha Test Success Criteria**:
- Passengers can see their quotes and status changes
- Accepting a quote creates a booking seamlessly
- All actions respect ownership (users only see/modify their own quotes)
- Error messages are clear and helpful

---

**Ready to Start?** Use the test data seeding script to populate quotes for testing:

```powershell
.\Scripts\Seed-Quotes.ps1
```

This creates 7 quotes with different statuses for comprehensive testing.

**Questions?** Contact the backend team or refer to the comprehensive API documentation in `Docs/15-Quote-Lifecycle.md`.

---

**Last Updated**: January 27, 2026  
**Status**: ? Ready for Implementation  
**Backend Version**: Phase Alpha Complete
