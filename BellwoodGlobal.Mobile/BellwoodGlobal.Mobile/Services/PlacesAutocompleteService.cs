using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BellwoodGlobal.Mobile.Models.Places;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Implementation of Google Places Autocomplete (New) API integration.
/// Handles session tokens, debouncing, quota tracking, and error handling.
/// Uses dynamic GPS-based location biasing for relevant results.
/// </summary>
public sealed class PlacesAutocompleteService : IPlacesAutocompleteService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocationPickerService _locationPicker;
    private readonly IPlacesUsageTracker _usageTracker; // NEW: Phase 7 usage tracking
    private readonly string _apiKey;

    // Quota tracking (persistent storage keys)
    private const string QuotaDateKey = "PlacesQuota_Date";
    private const string AutocompleteCountKey = "PlacesQuota_AutocompleteCount";
    private const string DetailsCountKey = "PlacesQuota_DetailsCount";
    private const string DisabledUntilKey = "PlacesQuota_DisabledUntil";

    // Quota limits (conservative daily limits)
    private const int DailyAutocompleteLimit = 1000; // Adjust based on your quota
    private const int DailyDetailsLimit = 500;

    // Rate limiting
    private DateTime _lastAutocompleteRequest = DateTime.MinValue;
    private const int MinMillisecondsBetweenRequests = 100; // 10 requests per second max

    // Debouncing state
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 300;

    // Location biasing cache
    private AppLocation? _cachedUserLocation;
    private DateTime _locationCacheTime = DateTime.MinValue;
    private static readonly TimeSpan LocationCacheExpiry = TimeSpan.FromMinutes(5);

    public PlacesAutocompleteService(
        IHttpClientFactory httpClientFactory,
        ILocationPickerService locationPicker,
        IPlacesUsageTracker usageTracker) // NEW: Phase 7
    {
        _httpClientFactory = httpClientFactory;
        _locationPicker = locationPicker;
        _usageTracker = usageTracker; // NEW: Phase 7

        // Get API key from AndroidManifest.xml or platform config
        // For now, using the key from AndroidManifest.xml
        // TODO: Move to secure config/secrets management
        _apiKey = "AIzaSyCDu1jdljMdXvcl9tG7O6cJBw8f2h0sUIY";

#if DEBUG
        Debug.WriteLine("[PlacesAutocompleteService] Initialized with dynamic location biasing and usage tracking");
#endif
    }

    /// <inheritdoc />
    public string GenerateSessionToken()
    {
        return Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public async Task<AutocompletePrediction[]> GetPredictionsAsync(
        string input,
        string sessionToken,
        CancellationToken ct = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(input))
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] GetPredictionsAsync: Empty input");
#endif
            return Array.Empty<AutocompletePrediction>();
        }

        // Minimum length check
        if (input.Length < 3)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] GetPredictionsAsync: Input too short ({input.Length} chars)");
#endif
            return Array.Empty<AutocompletePrediction>();
        }

        // NEW: Phase 7 - Check if autocomplete is disabled due to quota
        if (_usageTracker.IsAutocompleteDisabled())
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] GetPredictionsAsync: Autocomplete DISABLED (quota exceeded)");
#endif
            return Array.Empty<AutocompletePrediction>();
        }

        // NEW: Phase 7 - Check rate limiting
        if (_usageTracker.IsRateLimited())
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] GetPredictionsAsync: RATE LIMITED");
#endif
            await Task.Delay(500, ct); // Brief pause
            return Array.Empty<AutocompletePrediction>();
        }

        // Rate limiting (existing)
        await EnforceRateLimitAsync(ct);

        // NEW: Phase 7 - Record request
        _usageTracker.RecordAutocompleteRequest();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("places");

            // Build request with dynamic location biasing
            var requestBody = await BuildAutocompleteRequestAsync(input, sessionToken, ct);

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Autocomplete request: '{input}' (session: {sessionToken[..8]}...)");
#endif

            var response = await client.PostAsync(
                "v1/places:autocomplete",
                content,
                ct);

            stopwatch.Stop();

            // Log metrics
            LogRequest("Autocomplete", response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                // NEW: Phase 7 - Record error
                _usageTracker.RecordError();
                await HandleErrorResponseAsync("Autocomplete", response);
                return Array.Empty<AutocompletePrediction>();
            }

            var result = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(
                cancellationToken: ct);

            var predictions = result?.GetPredictions().ToArray() ?? Array.Empty<AutocompletePrediction>();

#if DEBUG
            // Keep a small, non-noisy diagnostic summary in DEBUG builds.
            Debug.WriteLine($"[PlacesAutocompleteService] Autocomplete returned {predictions.Length} predictions in {stopwatch.ElapsedMilliseconds}ms");
            if (predictions.Length > 0)
            {
                var p0 = predictions[0];
                Debug.WriteLine($"[PlacesAutocompleteService] Example: text='{TruncateForLog(p0.Text?.Text, 120)}' main='{TruncateForLog(p0.StructuredFormat?.MainText?.Text, 120)}' secondary='{TruncateForLog(p0.StructuredFormat?.SecondaryText?.Text, 120)}'");
            }
#endif

            return predictions;
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] Autocomplete request cancelled");
#endif
            return Array.Empty<AutocompletePrediction>();
        }
        catch (HttpRequestException ex)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Autocomplete network error: {ex.Message}");
#endif
            // NEW: Phase 7 - Record error
            _usageTracker.RecordError();
            LogError("Autocomplete", "NetworkError", ex.Message);
            return Array.Empty<AutocompletePrediction>();
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Autocomplete error: {ex}");
#endif
            // NEW: Phase 7 - Record error
            _usageTracker.RecordError();
            LogError("Autocomplete", "UnexpectedError", ex.Message);
            return Array.Empty<AutocompletePrediction>();
        }
    }

    /// <inheritdoc />
    public async Task<PlaceDetails?> GetPlaceDetailsAsync(
        string placeId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] GetPlaceDetailsAsync: Empty placeId");
#endif
            return null;
        }

        // NEW: Phase 7 - Check if disabled
        if (_usageTracker.IsAutocompleteDisabled())
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] GetPlaceDetailsAsync: Autocomplete DISABLED (quota exceeded)");
#endif
            return null;
        }

        // NEW: Phase 7 - Record Place Details call
        _usageTracker.RecordPlaceDetailsCall();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("places");

            // Field mask: only request fields we need (saves on cost)
            var fieldMask = "id,displayName,formattedAddress,location,types";

#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Place Details request: {placeId}");
#endif

            var request = new HttpRequestMessage(HttpMethod.Get, $"v1/places/{placeId}");
            request.Headers.Add("X-Goog-FieldMask", fieldMask);

            var response = await client.SendAsync(request, ct);

            stopwatch.Stop();

            // Log metrics
            LogRequest("PlaceDetails", response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                // NEW: Phase 7 - Record error
                _usageTracker.RecordError();
                await HandleErrorResponseAsync("PlaceDetails", response);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<PlaceDetails>(
                cancellationToken: ct);

#if DEBUG
            if (result != null)
            {
                Debug.WriteLine($"[PlacesAutocompleteService] Place Details returned: {result.FormattedAddress} in {stopwatch.ElapsedMilliseconds}ms");
            }
#endif

            return result;
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] Place Details request cancelled");
#endif
            return null;
        }
        catch (HttpRequestException ex)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Place Details network error: {ex.Message}");
#endif
            // NEW: Phase 7 - Record error
            _usageTracker.RecordError();
            LogError("PlaceDetails", "NetworkError", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Place Details error: {ex}");
#endif
            // NEW: Phase 7 - Record error
            _usageTracker.RecordError();
            LogError("PlaceDetails", "UnexpectedError", ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<AutocompletePrediction[]> SearchLocationsAsync(
        string input,
        string sessionToken,
        CancellationToken ct = default)
    {
        // Simple wrapper - predictions already contain location info
        return GetPredictionsAsync(input, sessionToken, ct);
    }

    /// <inheritdoc />
    public async Task<AppLocation?> GetLocationFromPlaceIdAsync(
        string placeId,
        CancellationToken ct = default)
    {
        var details = await GetPlaceDetailsAsync(placeId, ct);
        return details?.ToLocation();
    }

    // ========== Private Helper Methods ==========

    private async Task<object> BuildAutocompleteRequestAsync(
        string input,
        string sessionToken,
        CancellationToken ct)
    {
        // Try to get user's current location for biasing
        var userLocation = await GetCachedUserLocationAsync(ct);

        if (userLocation?.HasCoordinates == true)
        {
            // Use user's location for biasing (50km radius)
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Using location bias: {userLocation.Latitude:F4}, {userLocation.Longitude:F4}");
#endif

            return new
            {
                input = input,
                sessionToken = sessionToken,
                locationBias = new
                {
                    circle = new
                    {
                        center = new
                        {
                            latitude = userLocation.Latitude!.Value,
                            longitude = userLocation.Longitude!.Value
                        },
                        radius = 50000.0 // 50km radius
                    }
                },
                languageCode = "en",
                regionCode = "US"
            };
        }
        else
        {
            // Fallback: no location biasing, just region code
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] No user location available, using region-only biasing");
#endif

            return new
            {
                input = input,
                sessionToken = sessionToken,
                languageCode = "en",
                regionCode = "US" // Country-level biasing only
            };
        }
    }

    private async Task<AppLocation?> GetCachedUserLocationAsync(CancellationToken ct)
    {
        // Check cache first (valid for 5 minutes)
        if (_cachedUserLocation != null &&
            DateTime.UtcNow - _locationCacheTime < LocationCacheExpiry)
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] Using cached user location");
#endif
            return _cachedUserLocation;
        }

        // Try to get current location from GPS
        try
        {
#if DEBUG
            Debug.WriteLine("[PlacesAutocompleteService] Fetching user's current location for biasing...");
#endif

            _cachedUserLocation = await _locationPicker.GetCurrentLocationAsync(ct);
            _locationCacheTime = DateTime.UtcNow;

#if DEBUG
            if (_cachedUserLocation?.HasCoordinates == true)
            {
                Debug.WriteLine($"[PlacesAutocompleteService] Location cached: {_cachedUserLocation.Latitude:F4}, {_cachedUserLocation.Longitude:F4} ({_cachedUserLocation.Address})");
            }
            else
            {
                Debug.WriteLine("[PlacesAutocompleteService] Location unavailable (permission denied or GPS off)");
            }
#endif

            return _cachedUserLocation;
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"[PlacesAutocompleteService] Failed to get user location: {ex.Message}");
#endif
            // Location permission denied, GPS unavailable, or other error
            // Fall back to region-only biasing
            return null;
        }
    }

    private async Task EnforceRateLimitAsync(CancellationToken ct)
    {
        var elapsed = DateTime.UtcNow - _lastAutocompleteRequest;
        var remainingMs = MinMillisecondsBetweenRequests - (int)elapsed.TotalMilliseconds;

        if (remainingMs > 0)
        {
            await Task.Delay(remainingMs, ct);
        }

        _lastAutocompleteRequest = DateTime.UtcNow;
    }

    private void LogRequest(string endpoint, HttpStatusCode statusCode, long latencyMs)
    {
#if DEBUG
        Debug.WriteLine($"[PlacesAPI] {endpoint} | Status: {(int)statusCode} {statusCode} | Latency: {latencyMs}ms | Time: {DateTime.UtcNow:HH:mm:ss}");
#endif

        // TODO: Add structured logging to file or analytics service
        // For now, Debug output is sufficient
    }

    private async Task HandleErrorResponseAsync(string endpoint, HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        var reasonPhrase = response.ReasonPhrase ?? "Unknown";

        string errorBody = string.Empty;
        try
        {
            errorBody = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore errors reading error body
        }

#if DEBUG
        Debug.WriteLine($"[PlacesAPI] {endpoint} ERROR | Status: {statusCode} {reasonPhrase}");
        if (!string.IsNullOrWhiteSpace(errorBody))
        {
            Debug.WriteLine($"[PlacesAPI] Error body: {errorBody}");
        }
#endif

        // Handle specific error codes
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized: // 401
                LogError(endpoint, "Unauthorized", "Invalid API key or authentication failed");
                break;

            case HttpStatusCode.Forbidden: // 403
                LogError(endpoint, "Forbidden", "API key restrictions or quota issue");
                break;

            case (HttpStatusCode)429: // Too Many Requests
                LogError(endpoint, "QuotaExceeded", "Rate limit or quota exceeded");
                // Tracker will handle disabling
                break;

            case HttpStatusCode.InternalServerError: // 500
            case HttpStatusCode.BadGateway: // 502
            case HttpStatusCode.ServiceUnavailable: // 503
                LogError(endpoint, "ServerError", $"Google server error: {statusCode}");
                break;

            default:
                LogError(endpoint, "HttpError", $"HTTP {statusCode}: {reasonPhrase}");
                break;
        }
    }

    private void LogError(string endpoint, string errorType, string message)
    {
#if DEBUG
        Debug.WriteLine($"[PlacesAPI] ERROR | {endpoint} | {errorType} | {message}");
#endif

        // TODO: Add to structured error log
        // For now, Debug output is sufficient
    }

#if DEBUG
    private static string TruncateForLog(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max] + "...";
    }
#endif
}
