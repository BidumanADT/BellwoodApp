using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Result from a location picker operation.
/// </summary>
public sealed class LocationPickerResult
{
    /// <summary>
    /// Whether the user successfully selected a location.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The selected location with coordinates (if successful).
    /// </summary>
    public AppLocation? Location { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether the operation was cancelled by the user.
    /// </summary>
    public bool WasCancelled { get; init; }

    public static LocationPickerResult Cancelled() => new() { WasCancelled = true };
    public static LocationPickerResult Failed(string error) => new() { ErrorMessage = error };
    public static LocationPickerResult Succeeded(AppLocation location) => new() { Success = true, Location = location };
}

/// <summary>
/// Options for configuring the location picker behavior.
/// </summary>
public sealed class LocationPickerOptions
{
    /// <summary>
    /// Optional initial location to center the map on.
    /// </summary>
    public AppLocation? InitialLocation { get; init; }

    /// <summary>
    /// Optional initial address to search for.
    /// </summary>
    public string? InitialAddress { get; init; }

    /// <summary>
    /// Optional label to pre-fill for the location.
    /// </summary>
    public string? SuggestedLabel { get; init; }

    /// <summary>
    /// Whether to use the user's current location as the starting point.
    /// </summary>
    public bool UseCurrentLocation { get; init; } = true;

    /// <summary>
    /// Title for the location picker (used in prompts).
    /// </summary>
    public string Title { get; init; } = "Select Location";

    /// <summary>
    /// Whether to allow the user to search for addresses.
    /// </summary>
    public bool AllowSearch { get; init; } = true;

    /// <summary>
    /// Whether to try to geocode addresses to coordinates.
    /// </summary>
    public bool GeocodeAddress { get; init; } = true;
}

/// <summary>
/// Service for picking locations using native maps integration.
/// Supports cross-platform location selection via native map apps.
/// </summary>
public interface ILocationPickerService
{
    /// <summary>
    /// Launches the native map picker and returns the selected location.
    /// </summary>
    /// <param name="options">Configuration options for the picker.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the selected location or error information.</returns>
    Task<LocationPickerResult> PickLocationAsync(LocationPickerOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Opens the native maps app to display a specific location.
    /// </summary>
    /// <param name="location">The location to display.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OpenInMapsAsync(AppLocation location, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's current location.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current location or null if unavailable.</returns>
    Task<AppLocation?> GetCurrentLocationAsync(CancellationToken ct = default);

    /// <summary>
    /// Geocodes an address to coordinates.
    /// </summary>
    /// <param name="address">The address to geocode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Location with coordinates, or null if geocoding failed.</returns>
    Task<AppLocation?> GeocodeAddressAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Reverse geocodes coordinates to an address.
    /// </summary>
    /// <param name="latitude">Latitude coordinate.</param>
    /// <param name="longitude">Longitude coordinate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Location with address, or null if reverse geocoding failed.</returns>
    Task<AppLocation?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>
    /// Opens directions in the native maps app.
    /// </summary>
    /// <param name="from">Starting location (null for current location).</param>
    /// <param name="to">Destination location.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OpenDirectionsAsync(AppLocation? from, AppLocation to, CancellationToken ct = default);

    /// <summary>
    /// Checks if the device supports native map integration.
    /// </summary>
    bool IsMapIntegrationAvailable { get; }

    /// <summary>
    /// Checks if location services are available and enabled.
    /// </summary>
    Task<bool> IsLocationAvailableAsync();
}
