using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public interface IProfileService
{
    // ========== Booker ==========

    /// <summary>
    /// Clears all cached state (booker, passengers, locations) and resets cooldown timers.
    /// Call after logout or before loading a new user's data.
    /// </summary>
    void Reset();

    /// <summary>
    /// Fetches the booker profile from GET /profile and caches it.
    /// Idempotent: subsequent calls are no-ops once loaded.
    /// Safe to call from every OnAppearing.
    /// </summary>
    Task LoadProfileAsync();

    /// <summary>
    /// True once LoadProfileAsync has completed at least once (success or failure).
    /// </summary>
    bool IsProfileLoaded { get; }

    /// <summary>
    /// Returns the cached booker as a Passenger, or null if not yet loaded / profile not found.
    /// </summary>
    Passenger? GetBooker();

    // ========== Saved Passengers ==========

    /// <summary>
    /// Returns the in-memory cached list of saved passengers. Fast, synchronous.
    /// Call LoadSavedPassengersAsync first to populate.
    /// </summary>
    IReadOnlyList<Passenger> GetSavedPassengers();

    /// <summary>
    /// Fetches saved passengers from GET /profile/passengers, refreshes the in-memory list,
    /// and caches the result to Preferences for offline use.
    /// Falls back to Preferences cache if the API is unreachable.
    /// Safe to call from every OnAppearing.
    /// </summary>
    Task LoadSavedPassengersAsync();

    /// <summary>
    /// Creates a saved passenger via POST /profile/passengers.
    /// On success, refreshes the in-memory list and Preferences cache.
    /// Returns the new Passenger, or null on failure.
    /// </summary>
    Task<Passenger?> AddSavedPassengerAsync(string firstName, string lastName, string? phoneNumber, string? emailAddress);

    /// <summary>
    /// Updates a saved passenger via PUT /profile/passengers/{id}.
    /// Returns true on success.
    /// </summary>
    Task<bool> UpdateSavedPassengerAsync(string passengerId, string firstName, string lastName, string? phoneNumber, string? emailAddress);

    /// <summary>
    /// Deletes a saved passenger via DELETE /profile/passengers/{id}.
    /// Returns true on success (including 404, which means already gone).
    /// </summary>
    Task<bool> DeleteSavedPassengerAsync(string passengerId);

    // ========== Saved Locations ==========

    /// <summary>
    /// Returns the in-memory cached list of saved locations. Fast, synchronous.
    /// Call LoadSavedLocationsAsync first to populate.
    /// </summary>
    IReadOnlyList<Models.Location> GetSavedLocations();

    /// <summary>
    /// Fetches saved locations from GET /profile/locations, refreshes the in-memory list,
    /// and caches the result to Preferences for offline use.
    /// Falls back to Preferences cache if the API is unreachable.
    /// Safe to call from every OnAppearing.
    /// </summary>
    Task LoadSavedLocationsAsync();

    /// <summary>
    /// Creates a saved location via POST /profile/locations.
    /// On success, refreshes the in-memory list and Preferences cache.
    /// Returns the new Location, or null on failure.
    /// </summary>
    Task<Models.Location?> AddSavedLocationAsync(string label, string address, double latitude, double longitude, bool isFavorite);

    /// <summary>
    /// Updates a saved location via PUT /profile/locations/{id}.
    /// Returns true on success.
    /// </summary>
    Task<bool> UpdateSavedLocationAsync(string locationId, string label, string address, double latitude, double longitude, bool isFavorite);

    /// <summary>
    /// Deletes a saved location via DELETE /profile/locations/{id}.
    /// Returns true on success (including 404, which means already gone).
    /// </summary>
    Task<bool> DeleteSavedLocationAsync(string locationId);

    /// <summary>
    /// Gets a location by its ID from the in-memory cache.
    /// </summary>
    Models.Location? GetLocationById(string locationId);

    /// <summary>
    /// Gets favorite locations from the in-memory cache, ordered by descending useCount.
    /// </summary>
    IReadOnlyList<Models.Location> GetFavoriteLocations();
}
