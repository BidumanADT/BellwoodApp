namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Represents a saved location with optional coordinates for precise mapping.
/// </summary>
public class Location
{
    /// <summary>
    /// Unique identifier for the location.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// User-friendly label (e.g., "Home", "Work", "O'Hare").
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Full street address.
    /// </summary>
    public string Address { get; set; } = "";

    /// <summary>
    /// Latitude coordinate (null if not geocoded).
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate (null if not geocoded).
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Optional place ID from mapping service (Google Place ID, etc.).
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// Indicates if this location was verified via map selection.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Whether this is a favorite/frequently used location.
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// When the location was last updated.
    /// </summary>
    public DateTime? LastUpdatedUtc { get; set; }

    /// <summary>
    /// Number of times this location has been used.
    /// </summary>
    public int UseCount { get; set; }

    /// <summary>
    /// Returns true if this location has valid coordinates.
    /// </summary>
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    /// <summary>
    /// Gets coordinates as a tuple, or null if not available.
    /// </summary>
    public (double Lat, double Lng)? Coordinates =>
        HasCoordinates ? (Latitude!.Value, Longitude!.Value) : null;

    public override string ToString()
        => string.IsNullOrWhiteSpace(Label) ? Address : $"{Label} - {Address}";
}
