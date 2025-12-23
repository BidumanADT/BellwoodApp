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

    /// <summary>
    /// Driver's unique identifier.
    /// </summary>
    public string? DriverUid { get; set; }

    /// <summary>
    /// Driver's display name.
    /// </summary>
    public string? DriverName { get; set; }
}

/// <summary>
/// Response from the passenger location endpoint when tracking has not started yet.
/// </summary>
public sealed class PassengerLocationResponse
{
    /// <summary>
    /// The ride ID being tracked.
    /// </summary>
    public string RideId { get; set; } = "";

    /// <summary>
    /// Indicates if tracking is currently active.
    /// </summary>
    public bool TrackingActive { get; set; }

    /// <summary>
    /// Message explaining tracking status (e.g., "Driver has not started tracking yet").
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Current ride status (Scheduled, OnRoute, etc.).
    /// </summary>
    public string? CurrentStatus { get; set; }

    /// <summary>
    /// Location data (only present when TrackingActive is true).
    /// </summary>
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime? Timestamp { get; set; }

    public double? Heading { get; set; }

    public double? Speed { get; set; }

    public double? Accuracy { get; set; }

    public double? AgeSeconds { get; set; }

    public string? DriverUid { get; set; }

    public string? DriverName { get; set; }

    /// <summary>
    /// Converts this response to a DriverLocation if tracking is active.
    /// </summary>
    public DriverLocation? ToDriverLocation()
    {
        if (!TrackingActive || Latitude == null || Longitude == null || Timestamp == null)
            return null;

        return new DriverLocation
        {
            RideId = RideId,
            Latitude = Latitude.Value,
            Longitude = Longitude.Value,
            TimestampUtc = Timestamp.Value,
            AgeSeconds = (int)(AgeSeconds ?? 0),
            Heading = Heading,
            SpeedKmh = Speed,
            DriverUid = DriverUid,
            DriverName = DriverName
        };
    }
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
    /// <summary>Tracking not started yet (driver hasn't begun the ride).</summary>
    NotStarted,
    /// <summary>An error occurred.</summary>
    Error,
    /// <summary>Ride is no longer active (completed/cancelled).</summary>
    Ended,
    /// <summary>Unauthorized to view this ride.</summary>
    Unauthorized
}
