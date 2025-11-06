using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Services;

public interface IRideService
{
    Task<IReadOnlyList<Ride>> GetHistoryAsync(CancellationToken ct = default);
    Task<string> CreateAsync(BookRideRequest request);
    Task<IReadOnlyList<RideListItem>> ListAsync();
}
public record BookRideRequest(
    string PickupAddress,
    string DropoffAddress,
    DateTime PickupTime
);

public record RideListItem(
    string Id,
    DateTime PickupTime,
    string PickupAddress,
    string DropoffAddress,
    string Status,
    decimal Price
);
