using System.Text.Json.Serialization;

namespace BellwoodGlobal.Mobile.Models.Places;

/// <summary>
/// Represents a single prediction from the Google Places Autocomplete (New) API.
/// Note: The response shape for Places API (New) uses:
///   - placePrediction.placeId
///   - placePrediction.text.text
///   - placePrediction.structuredFormat.mainText.text
///   - placePrediction.structuredFormat.secondaryText.text
/// and may not include a top-level "description" field.
/// </summary>
public sealed class AutocompletePrediction
{
    /// <summary>
    /// Resource name for this place (e.g., "places/ChIJ...").
    /// </summary>
    [JsonPropertyName("place")]
    public string? Place { get; set; }

    /// <summary>
    /// Unique identifier for this place. Use this to fetch full details.
    /// </summary>
    [JsonPropertyName("placeId")]
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>
    /// Full one-line text for the prediction (e.g., "123 Main Street, San Francisco, CA").
    /// </summary>
    [JsonPropertyName("text")]
    public PredictionText? Text { get; set; }

    /// <summary>
    /// Split main/secondary fields returned as "structuredFormat".
    /// </summary>
    [JsonPropertyName("structuredFormat")]
    public StructuredFormat? StructuredFormat { get; set; }

    /// <summary>
    /// Types of this place (e.g., "street_address", "airport", "establishment").
    /// </summary>
    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }

    /// <summary>
    /// Legacy/optional field. The Places API (New) payload often does NOT include "description".
    /// Kept for backwards compatibility and safe UI fallbacks.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    // ========== Computed UI-Friendly Properties ==========

    /// <summary>
    /// One-line display text that should always be non-empty for UI binding.
    /// </summary>
    [JsonIgnore]
    public string DisplayText =>
        Coalesce(
            Text?.Text,
            // Some older payloads may supply the full line via Description
            Description,
            // As a last resort, build from structured format
            JoinMainSecondary(StructuredFormat?.MainText?.Text, StructuredFormat?.SecondaryText?.Text),
            PlaceId,
            "(unknown place)");

    /// <summary>
    /// Main line for UI (street/place name). Prefers structuredFormat.mainText.text.
    /// </summary>
    [JsonIgnore]
    public string MainTextDisplay =>
        Coalesce(
            StructuredFormat?.MainText?.Text,
            // fallback: split from full line
            SplitFirstSegment(Text?.Text ?? Description),
            Text?.Text,
            Description,
            PlaceId);

    /// <summary>
    /// Secondary line for UI (city/state/etc.). Prefers structuredFormat.secondaryText.text.
    /// </summary>
    [JsonIgnore]
    public string SecondaryTextDisplay =>
        Coalesce(
            StructuredFormat?.SecondaryText?.Text,
            // fallback: remainder after comma
            SplitRemainder(Text?.Text ?? Description),
            string.Empty);

    public override string ToString() => DisplayText;

    private static string Coalesce(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return string.Empty;
    }

    private static string SplitFirstSegment(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return string.Empty;
        var parts = full.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : full.Trim();
    }

    private static string SplitRemainder(string? full)
    {
        if (string.IsNullOrWhiteSpace(full)) return string.Empty;
        var parts = full.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length <= 1 ? string.Empty : string.Join(", ", parts.Skip(1));
    }

    private static string JoinMainSecondary(string? main, string? secondary)
    {
        if (!string.IsNullOrWhiteSpace(main) && !string.IsNullOrWhiteSpace(secondary))
            return $"{main.Trim()}, {secondary.Trim()}";
        return !string.IsNullOrWhiteSpace(main) ? main!.Trim() : (secondary ?? string.Empty).Trim();
    }
}

/// <summary>
/// Prediction text wrapper returned under "text".
/// </summary>
public sealed class PredictionText
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    // We don't currently use matches, but keep it for completeness.
    [JsonPropertyName("matches")]
    public List<TextMatch>? Matches { get; set; }
}

public sealed class TextMatch
{
    [JsonPropertyName("startOffset")]
    public int? StartOffset { get; set; }

    [JsonPropertyName("endOffset")]
    public int? EndOffset { get; set; }
}

/// <summary>
/// structuredFormat wrapper. This is where main/secondary are actually returned by Places API (New).
/// </summary>
public sealed class StructuredFormat
{
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

    [JsonPropertyName("matches")]
    public List<TextMatch>? Matches { get; set; }
}
