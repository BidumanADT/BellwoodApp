# CurrentRideStatus Support in Passenger App

## Issue Summary

The passenger app bookings dashboard was showing "Scheduled" status even after drivers changed the ride status to "OnRoute", "Arrived", or other driver-specific statuses. This was because:

1. The `BookingListItem` model did not include the `CurrentRideStatus` property
2. The bookings page only displayed the booking-level `Status` property
3. Driver-specific statuses (OnRoute, Arrived, PassengerOnboard) were not mapped in the UI

## Changes Made

### 1. Model Updates (`BookingClientModels.cs`)

Added `CurrentRideStatus` property to `BookingListItem`:

```csharp
public sealed class BookingListItem
{
    // ...existing properties...
    
    /// <summary>
    /// Current driver/ride-specific status (OnRoute, Arrived, PassengerOnboard, etc.)
    /// Used for real-time driver tracking status updates.
    /// If populated, this should be displayed instead of Status.
    /// </summary>
    public string? CurrentRideStatus { get; set; }
}
```

### 2. BookingsPage Updates (`BookingsPage.xaml.cs`)

#### Added Driver-Specific Status Mappings

```csharp
private static readonly Dictionary<string, string> DisplayStatusMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["OnRoute"] = "Driver En Route",
        ["Dispatched"] = "Dispatched",
        ["EnRoute"] = "En Route",
        ["Arrived"] = "Driver Arrived",
        ["PassengerOnboard"] = "Passenger On Board",
        // ...other statuses...
    };
```

#### Added Helper Method to Prefer CurrentRideStatus

```csharp
private static string GetEffectiveStatus(BookingListItem b)
{
    // Prefer CurrentRideStatus (driver-specific status) when populated
    var statusToUse = !string.IsNullOrWhiteSpace(b.CurrentRideStatus) 
        ? b.CurrentRideStatus 
        : b.Status;
    
    return ToDisplayStatus(statusToUse);
}
```

#### Updated RowVm to Use Effective Status

```csharp
public static RowVm From(BookingListItem b)
{
    var displayStatus = GetEffectiveStatus(b); // Now uses CurrentRideStatus when available
    // ...rest of method...
}
```

#### Updated Color Mappings

```csharp
private static Color StatusColorForDisplay(string display)
{
    var d = (display ?? "").ToLowerInvariant();
    return d switch
    {
        "driver en route" or "dispatched" or "en route" => TryGetColor("BellwoodGold", Colors.Gold),
        "driver arrived" or "arrived" => TryGetColor("BellwoodGold", Colors.Gold),
        "passenger on board" or "passengeronboard" => TryGetColor("BellwoodGold", Colors.Gold),
        // ...other statuses...
    };
}
```

### 3. BookingDetailPage Updates (`BookingDetailPage.xaml.cs`)

#### Updated Bind Method to Use CurrentRideStatus

```csharp
private void Bind(Models.BookingDetail d)
{
    // Prefer CurrentRideStatus (driver-specific) over Status when available
    var effectiveStatus = !string.IsNullOrWhiteSpace(d.CurrentRideStatus) 
        ? d.CurrentRideStatus 
        : d.Status;
    
    string displayStatus = ToDisplayStatus(effectiveStatus);
    StatusChip.Text = displayStatus;
    // ...rest of method...
}
```

#### Added Driver-Specific Status Mappings

Same mappings as BookingsPage for consistency.

#### Updated Trackable Statuses

```csharp
private static bool IsTrackableStatus(string? status)
{
    return status.Equals("OnRoute", StringComparison.OrdinalIgnoreCase) ||
           status.Equals("Arrived", StringComparison.OrdinalIgnoreCase) ||
           status.Equals("PassengerOnboard", StringComparison.OrdinalIgnoreCase) ||
           // ...other trackable statuses...
}
```

## How It Works

### Status Priority Logic

1. **Check `CurrentRideStatus` first**: If the booking has a driver-specific status (e.g., "OnRoute", "Arrived"), use that
2. **Fall back to `Status`**: If `CurrentRideStatus` is null or empty, use the booking-level status (e.g., "Scheduled", "Confirmed")

### Display Flow

```
API Response ? BookingListItem.CurrentRideStatus ? GetEffectiveStatus() ? ToDisplayStatus() ? UI Display
                                ? (if null)
                    BookingListItem.Status
```

### Example Scenarios

| Booking Status | CurrentRideStatus | Displayed As |
|----------------|-------------------|--------------|
| Scheduled | null | "Scheduled" |
| Scheduled | OnRoute | "Driver En Route" |
| Scheduled | Arrived | "Driver Arrived" |
| Scheduled | PassengerOnboard | "Passenger On Board" |
| Completed | null | "Completed" |

## Backend Requirements

The backend API must populate `CurrentRideStatus` in the `/bookings/list` endpoint response when a driver has updated the ride status. Example response:

```json
{
  "id": "booking-123",
  "status": "Scheduled",
  "currentRideStatus": "OnRoute",
  "passengerName": "John Doe",
  "pickupDateTime": "2024-01-15T10:00:00Z",
  // ...other fields...
}
```

## Testing Checklist

- [ ] Bookings list shows "Scheduled" when no driver status is set
- [ ] Bookings list shows "Driver En Route" when driver sets status to OnRoute
- [ ] Bookings list shows "Driver Arrived" when driver arrives at pickup
- [ ] Bookings list shows "Passenger On Board" when passenger boards
- [ ] Booking detail page shows same status as bookings list
- [ ] Status chip color is gold for active driver statuses
- [ ] "Track Driver" banner appears for trackable statuses (OnRoute, Arrived, PassengerOnboard)
- [ ] Status updates in real-time when driver changes status

## Related Files

- `BellwoodGlobal.Mobile/Models/BookingClientModels.cs`
- `BellwoodGlobal.Mobile/Pages/BookingsPage.xaml.cs`
- `BellwoodGlobal.Mobile/Pages/BookingDetailPage.xaml.cs`

## Future Enhancements

### Real-Time Status Updates

Consider adding SignalR or push notifications to update the bookings list when driver status changes, so users don't need to manually refresh:

```csharp
// Example: Subscribe to RideStatusChanged events
_hubConnection.On<string, string>("RideStatusChanged", (rideId, newStatus) =>
{
    // Update the booking in the observable collection
    var booking = _rows.FirstOrDefault(r => r.Id == rideId);
    if (booking != null)
    {
        booking.Status = MapDriverStatusToDisplay(newStatus);
        booking.StatusColor = StatusColorForDisplay(newStatus);
    }
});
```

## Notes

- This fix aligns the passenger app with the admin portal, which already uses `CurrentRideStatus`
- The `CurrentRideStatus` property is nullable to maintain backward compatibility with bookings that don't have driver status
- All status display logic is centralized in the `GetEffectiveStatus()` and `ToDisplayStatus()` methods for consistency
