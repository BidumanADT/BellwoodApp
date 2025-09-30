using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public interface IProfileService
{
    Passenger GetBooker();
    IReadOnlyList<Passenger> GetSavedPassengers();
    IReadOnlyList<Models.Location> GetSavedLocations();
}
