# Quote Lifecycle (Phase Alpha)

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Quote Lifecycle feature enables passengers to submit transportation quote requests, track their status in real-time, view dispatcher responses with estimated pricing, and accept quotes to create bookings. This is a **Phase Alpha** implementation using manual dispatcher pricing (placeholder until Limo Anywhere integration in Phase Beta).

**Key Capabilities**:
- ?? View all submitted quotes
- ?? Track quote status (Pending ? Acknowledged ? Responded ? Accepted)
- ?? View estimated pricing from dispatchers
- ? Accept quotes to create bookings
- ? Cancel unwanted quotes
- ?? Auto-refresh via 30-second polling

**Use Cases**:
- Passenger submits quote request for upcoming trip
- Dispatcher reviews and sends estimated price/ETA
- Passenger accepts quote, creating confirmed booking
- Passenger cancels quote if plans change

---

## ?? User Stories

**As a passenger**, I want to see all my quote requests in one place, so that I can track their status.

**As a passenger**, I want to receive estimated pricing from dispatchers, so that I can decide whether to accept the quote.

**As a passenger**, I want to accept a quote with one tap, so that I can quickly create a booking.

**As a passenger**, I want to cancel unwanted quotes, so that I don't clutter my quote list.

**As a dispatcher**, I want to send estimated prices to passengers, so that they can make informed decisions.

---

## ?? Benefits

### User Benefits

**Transparency**:
- See all quotes and their current status
- Track progress from submission to acceptance
- Clear messaging for each status

**Speed**:
- One-tap quote acceptance
- Automatic booking creation
- No manual data entry

**Control**:
- Cancel quotes before acceptance
- View full quote details anytime
- Real-time status updates

---

### Business Benefits

**Efficiency**:
- Automated quote-to-booking conversion
- Reduced manual processing
- Clear workflow for dispatchers

**Accountability**:
- Complete audit trail
- Ownership tracking
- Status history

**Scalability**:
- Handles multiple concurrent quotes
- Supports high volume
- Ready for future enhancements (WebSockets, real pricing)

---

## ??? Implementation

### Quote Status Flow

```
???????????????????????????????????????????
?  PENDING (Orange)                       ?
?  "Awaiting Response"                    ?
?  • Quote submitted                      ?
?  • Dispatcher hasn't acknowledged yet   ?
?  • Action: Cancel                       ?
???????????????????????????????????????????
                  ? Dispatcher acknowledges
???????????????????????????????????????????
?  ACKNOWLEDGED (Blue)                    ?
?  "Under Review"                         ?
?  • Dispatcher acknowledged receipt      ?
?  • Preparing price/ETA estimate         ?
?  • Action: Cancel                       ?
???????????????????????????????????????????
                  ? Dispatcher sends price
???????????????????????????????????????????
?  RESPONDED (Green)                      ?
?  "Response Received - $XX.XX"           ?
?  • Estimated price available            ?
?  • Estimated pickup time available      ?
?  • Actions: Accept, Cancel              ?
???????????????????????????????????????????
                  ? Passenger accepts
???????????????????????????????????????????
?  ACCEPTED (Gray)                        ?
?  "Booking Created"                      ?
?  • Booking created automatically        ?
?  • Quote is terminal (read-only)        ?
?  • Action: View Booking                 ?
???????????????????????????????????????????
```

**Terminal Statuses**:
- `Accepted` - Quote converted to booking
- `Cancelled` - Quote cancelled by passenger or staff

---

### Architecture Overview

```
???????????????????????????????????????????
?   QuoteDashboardPage.xaml               ?
?   (List of all user's quotes)           ?
???????????????????????????????????????????
                  ? Tap quote
                  ?
???????????????????????????????????????????
?   QuoteDetailPage.xaml                  ?
?   (Full details + actions)              ?
?   • Accept button                       ?
?   • Cancel button                       ?
???????????????????????????????????????????
                  ? Binds to
                  ?
???????????????????????????????????????????
?   AdminApi.cs (Service)                 ?
?   • GetQuotesAsync()                    ?
?   • GetQuoteAsync(id)                   ?
?   • AcceptQuoteAsync(id)                ?
?   • CancelQuoteAsync(id)                ?
???????????????????????????????????????????
                  ? HTTP calls
                  ?
???????????????????????????????????????????
?   AdminAPI (Backend)                    ?
?   • GET /quotes/list                    ?
?   • GET /quotes/{id}                    ?
?   • POST /quotes/{id}/accept            ?
?   • POST /quotes/{id}/cancel            ?
???????????????????????????????????????????
```

---

### Key Components

#### 1. QuoteDashboardPage

**Location**: `BellwoodGlobal.Mobile/Pages/QuoteDashboardPage.xaml`

**UI Components**:
- **CollectionView**: Lists all user's quotes
- **Filter Buttons**: "All", "Awaiting Response", "Response Received", "Cancelled"
- **SearchBar**: Search by passenger, location, vehicle
- **Pull-to-Refresh**: Manual refresh trigger
- **Status Badges**: Color-coded status indicators

**Features**:
- Auto-refresh every 30 seconds
- Filtering by status
- Search functionality
- Tap quote to view details

**Implementation**:
```csharp
public partial class QuoteDashboardPage : ContentPage
{
    private Timer _pollingTimer;
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Start polling
        _pollingTimer = new Timer(30000); // 30 seconds
        _pollingTimer.Elapsed += async (s, e) => await RefreshQuotesAsync();
        _pollingTimer.Start();
        
        // Initial load
        await RefreshQuotesAsync();
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop polling
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
    }
    
    private async Task RefreshQuotesAsync()
    {
        try
        {
            var quotes = await _adminApi.GetQuotesAsync(take: 100);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                QuotesList.ItemsSource = quotes;
                LastUpdatedLabel.Text = $"Updated {DateTime.Now:h:mm tt}";
            });
        }
        catch (Exception ex)
        {
            // Log error but don't interrupt user
            Debug.WriteLine($"Quote refresh failed: {ex.Message}");
        }
    }
}
```

---

#### 2. QuoteDetailPage

**Location**: `BellwoodGlobal.Mobile/Pages/QuoteDetailPage.xaml`

**UI Sections**:
- **Status Chip**: Large, color-coded status badge
- **Trip Details**: Pickup, dropoff, vehicle, passengers, bags
- **Dispatcher Response** (if responded): Price, ETA, notes
- **Passenger Contact**: Name, phone, email
- **Action Buttons**: Accept, Cancel (dynamic based on status)

**Dynamic UI Logic**:
```csharp
private void UpdateUIForStatus(QuoteDetail quote)
{
    switch (quote.Status)
    {
        case "Pending":
            StatusChip.Text = "? Awaiting Response";
            StatusChip.BackgroundColor = Color.FromArgb("#FFA500"); // Orange
            ResponseSection.IsVisible = false;
            AcceptButton.IsVisible = false;
            CancelButton.IsVisible = true;
            break;
            
        case "Acknowledged":
            StatusChip.Text = "?? Under Review";
            StatusChip.BackgroundColor = Color.FromArgb("#0000FF"); // Blue
            ResponseSection.IsVisible = false;
            AcceptButton.IsVisible = false;
            CancelButton.IsVisible = true;
            break;
            
        case "Responded":
            StatusChip.Text = $"? Response Received - ${quote.EstimatedPrice:F2}";
            StatusChip.BackgroundColor = Color.FromArgb("#008000"); // Green
            ResponseSection.IsVisible = true;
            EstimatedPriceLabel.Text = $"${quote.EstimatedPrice:F2}";
            EstimatedPickupLabel.Text = quote.EstimatedPickupTime?.ToString("MMM dd @ h:mm tt");
            NotesLabel.Text = quote.Notes ?? "No additional notes";
            AcceptButton.IsVisible = true;
            CancelButton.IsVisible = true;
            break;
            
        case "Accepted":
            StatusChip.Text = "?? Booking Created";
            StatusChip.BackgroundColor = Color.FromArgb("#808080"); // Gray
            ResponseSection.IsVisible = true;
            AcceptButton.IsVisible = false;
            CancelButton.IsVisible = false;
            ViewBookingButton.IsVisible = true;
            break;
            
        case "Cancelled":
            StatusChip.Text = "? Cancelled";
            StatusChip.BackgroundColor = Color.FromArgb("#FF0000"); // Red
            ResponseSection.IsVisible = false;
            AcceptButton.IsVisible = false;
            CancelButton.IsVisible = false;
            break;
    }
}
```

---

### API Integration

See `20-API-Integration.md` for complete API documentation.

**List Quotes**:
```csharp
// GET /quotes/list
var quotes = await _adminApi.GetQuotesAsync(take: 100);
// Returns: List<QuoteListItem>
```

**Get Quote Details**:
```csharp
// GET /quotes/{id}
var quote = await _adminApi.GetQuoteAsync(quoteId);
// Returns: QuoteDetail
```

**Accept Quote**:
```csharp
// POST /quotes/{id}/accept
var result = await _adminApi.AcceptQuoteAsync(quoteId);
// Returns: AcceptQuoteResponse { BookingId, QuoteStatus, ... }
```

**Cancel Quote**:
```csharp
// POST /quotes/{id}/cancel
await _adminApi.CancelQuoteAsync(quoteId);
// Returns: 200 OK (no body)
```

---

### Polling Strategy

**Rationale**: 
- Simple implementation (no WebSocket complexity)
- Works on all networks (firewall-friendly)
- Adequate for alpha testing
- Future: migrate to SignalR for real-time updates

**Implementation**:
```csharp
// 30-second polling on dashboard
private Timer _pollingTimer = new Timer(30000);

// Start when page appears
protected override void OnAppearing()
{
    _pollingTimer.Elapsed += OnPollElapsed;
    _pollingTimer.Start();
    await RefreshQuotesAsync(); // Initial load
}

// Stop when page disappears
protected override void OnDisappearing()
{
    _pollingTimer.Stop();
}

private async void OnPollElapsed(object sender, ElapsedEventArgs e)
{
    await RefreshQuotesAsync();
}

private async Task RefreshQuotesAsync()
{
    var quotes = await _adminApi.GetQuotesAsync();
    
    // Detect status changes
    var changedQuotes = DetectChanges(quotes);
    
    if (changedQuotes.Any())
    {
        // Show notification banner
        ShowNotificationBanner($"{changedQuotes.Count} quote(s) updated");
    }
    
    // Update UI
    MainThread.BeginInvokeOnMainThread(() =>
    {
        QuotesList.ItemsSource = quotes;
    });
}
```

---

## ?? Configuration

**Polling Interval**:

```json
// appsettings.json
{
  "QuotePollingIntervalSeconds": 30
}
```

**Usage**:
```csharp
var config = ServiceHelper.GetRequiredService<IConfigurationService>();
var intervalMs = config.QuotePollingIntervalSeconds * 1000;

_pollingTimer = new Timer(intervalMs);
```

**Recommended Values**:
- **Development**: 10-15 seconds (faster feedback)
- **Production**: 30 seconds (balance between freshness and server load)

---

## ?? Usage Examples

### Example 1: Accept Quote Flow

```csharp
private async void OnAcceptQuoteTapped(object sender, EventArgs e)
{
    // Disable button to prevent double-tap
    AcceptButton.IsEnabled = false;
    
    try
    {
        var result = await _adminApi.AcceptQuoteAsync(_quoteId);
        
        // Show success dialog
        var viewBooking = await DisplayAlert(
            "Success!",
            "Quote accepted! Your booking has been created.",
            "View Booking",
            "OK");
        
        if (viewBooking)
        {
            // Navigate to booking detail
            await Shell.Current.GoToAsync($"BookingDetailPage?id={result.BookingId}");
        }
        else
        {
            // Go back to dashboard
            await Shell.Current.GoToAsync("..");
        }
    }
    catch (InvalidOperationException ex)
    {
        // Business rule violation (e.g., wrong status)
        await DisplayAlert(
            "Cannot Accept Quote",
            ex.Message,
            "OK");
        
        // Re-enable button
        AcceptButton.IsEnabled = true;
    }
    catch (Exception ex)
    {
        // Generic error
        await DisplayAlert(
            "Error",
            $"Failed to accept quote: {ex.Message}",
            "OK");
        
        AcceptButton.IsEnabled = true;
    }
}
```

---

### Example 2: Cancel Quote Flow

```csharp
private async void OnCancelQuoteTapped(object sender, EventArgs e)
{
    // Confirmation dialog
    var confirm = await DisplayAlert(
        "Cancel Quote?",
        "Are you sure you want to cancel this quote request? This action cannot be undone.",
        "Yes, Cancel",
        "No");
    
    if (!confirm) return;
    
    try
    {
        await _adminApi.CancelQuoteAsync(_quoteId);
        
        await DisplayAlert(
            "Quote Cancelled",
            "Your quote request has been cancelled.",
            "OK");
        
        // Navigate back to dashboard
        await Shell.Current.GoToAsync("..");
    }
    catch (InvalidOperationException ex)
    {
        // Cannot cancel (e.g., already accepted)
        await DisplayAlert(
            "Cannot Cancel",
            ex.Message,
            "OK");
    }
}
```

---

### Example 3: Notification Banner

```csharp
private void ShowNotificationBanner(string message)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        // Show gold banner at top
        NotificationBanner.IsVisible = true;
        NotificationLabel.Text = message;
        
        // Auto-dismiss after 5 seconds
        Task.Delay(5000).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NotificationBanner.IsVisible = false;
            });
        });
    });
}
```

---

## ?? Phase Alpha Limitations

### ?? Placeholder Pricing

**Current Behavior**:
- Dispatchers **manually enter** estimated prices
- Prices are **placeholders only** (not actual Limo Anywhere quotes)
- Subject to change upon final confirmation

**User Communication**:
```
?? Estimated Price: $85.50
?? Placeholder price - final cost may vary based on actual booking details
```

**Future Enhancement** (Phase Beta):
- Integrate Limo Anywhere API
- Fetch real-time pricing
- Display actual quote from limo service

---

### ?? Polling Instead of Real-Time

**Current Behavior**:
- 30-second polling intervals
- Status updates have up to 30-second delay
- Not truly "real-time"

**User Impact**:
- Acceptable for alpha testing
- Slight delay before seeing status changes

**Future Enhancement** (Phase 3):
- Migrate to SignalR WebSockets
- Instant push notifications
- Real-time status updates

---

### ?? No Push Notifications

**Current Behavior**:
- User must have app open to see updates
- No background notifications

**Future Enhancement** (Phase 3):
- iOS/Android push notifications
- Alert when dispatcher responds
- "Quote accepted" confirmations

---

## ?? Troubleshooting

### Issue: Quotes not appearing

**Symptoms**:
- Empty quote list
- "No quotes found" message

**Possible Causes**:
1. User hasn't submitted any quotes yet
2. Network connectivity issue
3. JWT token expired

**Solutions**:
1. Verify user has submitted quotes (check AdminPortal)
2. Check network connection
3. Re-login to refresh token

---

### Issue: Cannot accept quote

**Symptoms**:
- "Cannot accept quote" error
- 400 Bad Request response

**Possible Causes**:
1. Quote not in "Responded" status yet
2. Quote already accepted or cancelled
3. User is not the quote owner

**Solutions**:
1. Check quote status (must be "Responded")
2. Refresh quote details to get latest status
3. Verify logged-in user matches quote creator

---

### Issue: Estimated price not showing

**Symptoms**:
- Price section is blank
- No price displayed in status badge

**Cause**: Quote is not in "Responded" status yet

**Solution**: Wait for dispatcher to respond (status will change to "Responded")

---

### Issue: Status not updating

**Symptoms**:
- Quote status stuck on old value
- Changes not reflecting

**Possible Causes**:
1. Polling not working (timer stopped)
2. Page not active (user navigated away)
3. Network issue

**Solutions**:
1. Pull-to-refresh manually
2. Close and reopen dashboard
3. Check network connection

---

## ?? Future Enhancements

### Planned (Phase Beta)

**1. Limo Anywhere Integration**:
- Replace placeholder pricing with real quotes
- Fetch actual availability and pricing
- Display accurate vehicle options

**2. WebSocket Real-Time Updates**:
- Replace polling with SignalR
- Instant status change notifications
- No 30-second delay

**3. Push Notifications**:
- Alert when dispatcher responds
- Notify when quote accepted
- Background notifications

---

### Nice-to-Have (v2.0)

**1. Quote Comparison**:
- View multiple quotes side-by-side
- Compare prices and vehicles
- Accept best option

**2. Quote History**:
- View all past quotes (including old/expired)
- Filter by date range
- Export quote history

**3. Favorite Routes**:
- Save frequent routes as templates
- One-tap quote for saved routes
- Pre-filled passenger details

**4. Share Quotes**:
- Share quote details via SMS/email
- Link to track quote status
- Notify others when accepted

---

## ?? Related Documentation

- **[00-README.md](00-README.md)** - Quick start & overview
- **[01-System-Architecture.md](01-System-Architecture.md)** - Architecture details
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing scenarios
- **[20-API-Integration.md](20-API-Integration.md)** - AdminAPI quote endpoints
- **[21-Data-Models.md](21-Data-Models.md)** - QuoteDraft, QuoteDetail models
- **[23-Security-Model.md](23-Security-Model.md)** - Ownership-based authorization
- **[31-Scripts-Reference.md](31-Scripts-Reference.md)** - Testing scripts for quote seeding
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common quote issues

---

**Last Updated**: January 27, 2026  
**Version**: 1.0 (Phase Alpha)  
**Status**: ? Production Ready
