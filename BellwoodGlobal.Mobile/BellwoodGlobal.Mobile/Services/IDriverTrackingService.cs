using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Service interface for fetching and managing real-time driver location tracking.
/// </summary>
public interface IDriverTrackingService
{
    /// <summary>
    /// Event fired when a new driver location is received.
    /// </summary>
    event EventHandler<DriverLocation>? LocationUpdated;

    /// <summary>
    /// Event fired when tracking state changes.
    /// </summary>
    event EventHandler<TrackingState>? StateChanged;

    /// <summary>
    /// Event fired when ETA is updated.
    /// </summary>
    event EventHandler<EtaResult>? EtaUpdated;

    /// <summary>
    /// Current tracking state.
    /// </summary>
    TrackingState CurrentState { get; }

    /// <summary>
    /// Most recent driver location, or null if unavailable.
    /// </summary>
    DriverLocation? LastKnownLocation { get; }

    /// <summary>
    /// Most recent ETA calculation, or null if unavailable.
    /// </summary>
    EtaResult? LastKnownEta { get; }

    /// <summary>
    /// Starts polling for driver location updates for the specified ride.
    /// </summary>
    /// <param name="rideId">The ride ID to track.</param>
    /// <param name="pickupLatitude">Pickup location latitude for ETA calculation.</param>
    /// <param name="pickupLongitude">Pickup location longitude for ETA calculation.</param>
    /// <param name="pollingIntervalMs">Polling interval in milliseconds (default: 15000).</param>
    Task StartTrackingAsync(string rideId, double pickupLatitude, double pickupLongitude, int pollingIntervalMs = 15000);

    /// <summary>
    /// Stops the current tracking session.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Manually fetches the latest driver location (one-time fetch).
    /// </summary>
    /// <param name="rideId">The ride ID to fetch location for.</param>
    /// <returns>The driver location, or null if unavailable.</returns>
    Task<DriverLocation?> GetDriverLocationAsync(string rideId);

    /// <summary>
    /// Calculates ETA from driver's current location to pickup.
    /// </summary>
    /// <param name="driverLocation">The driver's current location.</param>
    /// <param name="pickupLatitude">Pickup latitude.</param>
    /// <param name="pickupLongitude">Pickup longitude.</param>
    /// <returns>ETA result with estimated time and distance.</returns>
    EtaResult CalculateEta(DriverLocation driverLocation, double pickupLatitude, double pickupLongitude);
}
