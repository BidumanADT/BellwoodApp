using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public interface IProfileService
{
    // ========== Booker =========
    Passenger GetBooker();

    // ========== Passengers =========
    IReadOnlyList<Passenger> GetSavedPassengers();
    
    /// <summary>
    /// Adds a new passenger to the saved list.
    /// </summary>
    void AddPassenger(Passenger passenger);
    
    /// <summary>
    /// Removes a passenger from the saved list.
    /// </summary>
    bool RemovePassenger(string passengerId);

    // ========== Locations =========
    IReadOnlyList<Models.Location> GetSavedLocations();
    
    /// <summary>
    /// Adds a new location to the saved list.
    /// </summary>
    void AddLocation(Models.Location location);
    
    /// <summary>
    /// Updates an existing location.
    /// </summary>
    bool UpdateLocation(Models.Location location);
    
    /// <summary>
    /// Removes a location from the saved list.
    /// </summary>
    bool RemoveLocation(string locationId);
    
    /// <summary>
    /// Gets a location by its ID.
    /// </summary>
    Models.Location? GetLocationById(string locationId);
    
    /// <summary>
    /// Gets favorite/frequently used locations.
    /// </summary>
    IReadOnlyList<Models.Location> GetFavoriteLocations();
    
    /// <summary>
    /// Marks a location as favorite.
    /// </summary>
    void SetLocationFavorite(string locationId, bool isFavorite);
}
