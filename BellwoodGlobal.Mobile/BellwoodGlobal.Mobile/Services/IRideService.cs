using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Services;

public interface IRideService
{
    Task<IReadOnlyList<Ride>> GetHistoryAsync(CancellationToken ct = default);
}
