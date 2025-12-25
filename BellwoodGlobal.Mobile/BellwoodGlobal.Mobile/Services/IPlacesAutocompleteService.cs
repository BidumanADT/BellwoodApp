using BellwoodGlobal.Mobile.Models.Places;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Service for interacting with Google Places Autocomplete (New) API.
/// Provides real-time address suggestions and place details lookup.
/// </summary>
public interface IPlacesAutocompleteService
{
    /// <summary>
    /// Gets autocomplete predictions for a user's search query.
    /// </summary>
    /// <param name="input">User's search text (e.g., "123 Main").</param>
    /// <param name="sessionToken">Session token to group autocomplete requests for billing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of predictions, or empty array on failure.</returns>
    Task<AutocompletePrediction[]> GetPredictionsAsync(
        string input, 
        string sessionToken, 
        CancellationToken ct = default);

    /// <summary>
    /// Gets full place details (address, coordinates) for a selected place.
    /// </summary>
    /// <param name="placeId">Place ID from a prediction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Place details or null on failure.</returns>
    Task<PlaceDetails?> GetPlaceDetailsAsync(
        string placeId, 
        CancellationToken ct = default);

    /// <summary>
    /// Convenience method: Gets predictions and converts to app Location models.
    /// </summary>
    /// <param name="input">User's search text.</param>
    /// <param name="sessionToken">Session token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of predictions with basic location info.</returns>
    Task<AutocompletePrediction[]> SearchLocationsAsync(
        string input,
        string sessionToken,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience method: Gets place details and converts to app Location model.
    /// </summary>
    /// <param name="placeId">Place ID from a prediction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Location with coordinates or null on failure.</returns>
    Task<AppLocation?> GetLocationFromPlaceIdAsync(
        string placeId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a new session token (UUID v4).
    /// Use one token per autocomplete interaction (from first keystroke to selection).
    /// </summary>
    string GenerateSessionToken();
}
