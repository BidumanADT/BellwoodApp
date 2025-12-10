namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Represents the driver's current GPS location and tracking metadata.
/// </summary>
public sealed class DriverLocation
{
    /// <summary>
    /// The ride ID this location is associated with.
    /// </summary>
    public string RideId { get; set; } = "";

    /// <summary>
    /// Driver's current latitude.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Driver's current longitude.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// UTC timestamp when this location was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Age of the location data in seconds (server-computed).
    /// </summary>
    public int AgeSeconds { get; set; }

    /// <summary>
    /// Driver's heading/bearing in degrees (0-360), if available.
    /// </summary>
    public double? Heading { get; set; }

    /// <summary>
    /// Driver's speed in km/h, if available.
    /// </summary>
    public double? SpeedKmh { get; set; }

    /// <summary>
    /// Indicates if location data is considered stale (older than expected).
    /// </summary>
    public bool IsStale => AgeSeconds > 120; // > 2 minutes considered stale
}

/// <summary>
/// Result of an ETA calculation request.
/// </summary>
public sealed class EtaResult
{
    /// <summary>
    /// Estimated time of arrival in minutes.
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// Distance remaining in kilometers.
    /// </summary>
    public double DistanceKm { get; set; }

    /// <summary>
    /// Human-readable ETA text (e.g., "8 minutes away").
    /// </summary>
    public string DisplayText => EstimatedMinutes <= 1 ? "Arriving now" : $"{EstimatedMinutes} min away";

    /// <summary>
    /// Indicates if this is a rough estimate (no routing API available).
    /// </summary>
    public bool IsEstimate { get; set; }
}

/// <summary>
/// Tracking state for UI binding.
/// </summary>
public enum TrackingState
{
    /// <summary>Initial loading state.</summary>
    Loading,
    /// <summary>Actively tracking driver location.</summary>
    Tracking,
    /// <summary>Location temporarily unavailable.</summary>
    Unavailable,
    /// <summary>An error occurred.</summary>
    Error,
    /// <summary>Ride is no longer active (completed/cancelled).</summary>
    Ended
}
