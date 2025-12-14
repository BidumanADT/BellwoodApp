using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Service interface for monitoring ride status updates.
/// Supports both polling and future push notification integration.
/// </summary>
public interface IRideStatusService
{
    /// <summary>
    /// Event fired when ride status changes.
    /// </summary>
    event EventHandler<RideStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Starts monitoring a ride for status changes.
    /// </summary>
    /// <param name="rideId">The ride ID to monitor.</param>
    /// <param name="pollingIntervalMs">Polling interval in milliseconds (default: 30000).</param>
    Task StartMonitoringAsync(string rideId, int pollingIntervalMs = 30000);

    /// <summary>
    /// Stops monitoring the current ride.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets the current ride status.
    /// </summary>
    Task<string?> GetCurrentStatusAsync(string rideId);
}

/// <summary>
/// Event arguments for ride status changes.
/// </summary>
public sealed class RideStatusChangedEventArgs : EventArgs
{
    public string RideId { get; init; } = "";
    public string OldStatus { get; init; } = "";
    public string NewStatus { get; init; } = "";
    public bool IsTrackable => IsTrackableStatus(NewStatus);

    private static bool IsTrackableStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        return status.Equals("OnRoute", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("InProgress", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("EnRoute", StringComparison.OrdinalIgnoreCase);
    }
}
