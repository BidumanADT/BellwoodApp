using System.Text.Json.Serialization;

namespace BellwoodGlobal.Mobile.Models.Places;

/// <summary>
/// Response from Google Places Autocomplete (New) API.
/// </summary>
public sealed class AutocompleteResponse
{
    /// <summary>
    /// Array of predictions returned by the API.
    /// </summary>
    [JsonPropertyName("suggestions")]
    public List<SuggestionWrapper>? Suggestions { get; set; }

    /// <summary>
    /// Gets the predictions directly (unwrapping from suggestions).
    /// </summary>
    public IEnumerable<AutocompletePrediction> GetPredictions()
    {
        if (Suggestions == null) return Enumerable.Empty<AutocompletePrediction>();
        
        return Suggestions
            .Where(s => s.PlacePrediction != null)
            .Select(s => s.PlacePrediction!)
            .ToList();
    }
}

/// <summary>
/// Wrapper for each suggestion (Places API New format).
/// </summary>
public sealed class SuggestionWrapper
{
    [JsonPropertyName("placePrediction")]
    public AutocompletePrediction? PlacePrediction { get; set; }
}
