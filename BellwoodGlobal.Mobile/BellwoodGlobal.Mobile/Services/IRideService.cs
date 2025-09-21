using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public interface IRideService
{
    Task<IReadOnlyList<Ride>> GetHistoryAsync(CancellationToken ct = default);
}
