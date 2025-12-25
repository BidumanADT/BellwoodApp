using System.Text.Json.Serialization;

namespace BellwoodGlobal.Mobile.Models.Places;

/// <summary>
/// Represents a single prediction from the Google Places Autocomplete (New) API.
/// </summary>
public sealed class AutocompletePrediction
{
    /// <summary>
    /// Unique identifier for this place. Use this to fetch full details.
    /// </summary>
    [JsonPropertyName("placeId")]
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>
    /// Full text description of the prediction (e.g., "123 Main St, Chicago, IL, USA").
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Main text (usually the place name or street address).
    /// </summary>
    [JsonPropertyName("mainText")]
    public string? MainText { get; set; }

    /// <summary>
    /// Secondary text (usually city, state, country).
    /// </summary>
    [JsonPropertyName("secondaryText")]
    public string? SecondaryText { get; set; }

    /// <summary>
    /// Structured text representation with main and secondary parts.
    /// </summary>
    [JsonPropertyName("text")]
    public StructuredText? Text { get; set; }

    /// <summary>
    /// Types of this place (e.g., "street_address", "airport", "establishment").
    /// </summary>
    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }

    public override string ToString() => Description;
}

/// <summary>
/// Structured text with main and secondary components.
/// </summary>
public sealed class StructuredText
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("mainText")]
    public TextComponent? MainText { get; set; }

    [JsonPropertyName("secondaryText")]
    public TextComponent? SecondaryText { get; set; }
}

/// <summary>
/// Text component with display text.
/// </summary>
public sealed class TextComponent
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
