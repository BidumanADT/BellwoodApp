using System.Text.Json.Serialization;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.Models.Places;

/// <summary>
/// Full place details from Google Places API (New).
/// Contains display name, formatted address, and geographic coordinates.
/// </summary>
public sealed class PlaceDetails
{
    /// <summary>
    /// Unique place identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Display name of the place (e.g., "Willis Tower", "123 Main Street").
    /// </summary>
    [JsonPropertyName("displayName")]
    public DisplayName? DisplayName { get; set; }

    /// <summary>
    /// Full formatted address (e.g., "233 S Wacker Dr, Chicago, IL 60606, USA").
    /// </summary>
    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    /// <summary>
    /// Geographic location (latitude/longitude).
    /// </summary>
    [JsonPropertyName("location")]
    public LocationCoordinates? Location { get; set; }

    /// <summary>
    /// Types of this place (e.g., "street_address", "airport").
    /// </summary>
    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }

    /// <summary>
    /// Converts this PlaceDetails to the app's Location model.
    /// </summary>
    public AppLocation ToLocation()
    {
        var displayNameText = DisplayName?.Text ?? string.Empty;
        var formattedAddr = FormattedAddress ?? string.Empty;

        // Use display name as label, or extract street from formatted address
        var label = !string.IsNullOrWhiteSpace(displayNameText) 
            ? displayNameText 
            : ExtractStreetFromAddress(formattedAddr);

        return new AppLocation
        {
            Label = label,
            Address = formattedAddr,
            Latitude = Location?.Latitude,
            Longitude = Location?.Longitude,
            PlaceId = Id,
            IsVerified = true,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }

    private static string ExtractStreetFromAddress(string address)
    {
        // Simple extraction: take first part before first comma
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        
        var firstComma = address.IndexOf(',');
        return firstComma > 0 ? address[..firstComma].Trim() : address;
    }
}

/// <summary>
/// Display name with text and language code.
/// </summary>
public sealed class DisplayName
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Geographic coordinates (latitude and longitude).
/// </summary>
public sealed class LocationCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
