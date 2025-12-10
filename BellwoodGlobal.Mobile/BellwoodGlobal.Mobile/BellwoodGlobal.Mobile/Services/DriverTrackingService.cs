using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Service for fetching and managing real-time driver location tracking.
/// Uses polling to fetch driver GPS coordinates from the backend.
/// </summary>
public sealed class DriverTrackingService : IDriverTrackingService, IDisposable
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Average driving speed for ETA estimation (km/h) when no routing API is available
    private const double AverageSpeedKmh = 35.0;

    public event EventHandler<DriverLocation>? LocationUpdated;
    public event EventHandler<TrackingState>? StateChanged;
    public event EventHandler<EtaResult>? EtaUpdated;

    public TrackingState CurrentState { get; private set; } = TrackingState.Loading;
    public DriverLocation? LastKnownLocation { get; private set; }
    public EtaResult? LastKnownEta { get; private set; }

    // Pickup coordinates for ETA calculation
    private double _pickupLatitude;
    private double _pickupLongitude;
    private string? _currentRideId;

    public DriverTrackingService(IHttpClientFactory httpFactory, IAuthService auth)
    {
        _http = httpFactory.CreateClient("admin");
        _auth = auth;
    }

    public async Task StartTrackingAsync(string rideId, double pickupLatitude, double pickupLongitude, int pollingIntervalMs = 15000)
    {
        // Stop any existing tracking
        StopTracking();

        _currentRideId = rideId;
        _pickupLatitude = pickupLatitude;
        _pickupLongitude = pickupLongitude;

        SetState(TrackingState.Loading);

        _pollingCts = new CancellationTokenSource();
        _pollingTask = PollLocationAsync(rideId, pollingIntervalMs, _pollingCts.Token);

        await Task.CompletedTask;
    }

    public void StopTracking()
    {
        if (_pollingCts != null)
        {
            _pollingCts.Cancel();
            _pollingCts.Dispose();
            _pollingCts = null;
        }

        _pollingTask = null;
        _currentRideId = null;

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[DriverTrackingService] Tracking stopped");
#endif
    }

    public async Task<DriverLocation?> GetDriverLocationAsync(string rideId)
    {
        try
        {
            var response = await _http.GetAsync($"/driver/location/{Uri.EscapeDataString(rideId)}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // No location data available
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] No location for ride {rideId}");
#endif
                return null;
            }

            response.EnsureSuccessStatusCode();

            var location = await response.Content.ReadFromJsonAsync<DriverLocation>(_jsonOptions);

#if DEBUG
            if (location != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DriverTrackingService] Location received: {location.Latitude:F6}, {location.Longitude:F6}, Age={location.AgeSeconds}s");
            }
#endif

            return location;
        }
        catch (HttpRequestException ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] HTTP error: {ex.Message}");
#endif
            return null;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] Error fetching location: {ex.Message}");
#endif
            return null;
        }
    }

    public EtaResult CalculateEta(DriverLocation driverLocation, double pickupLatitude, double pickupLongitude)
    {
        // Calculate distance using Haversine formula
        var distanceKm = CalculateDistanceKm(
            driverLocation.Latitude, driverLocation.Longitude,
            pickupLatitude, pickupLongitude);

        // Use driver's actual speed if available, otherwise use average
        var speedKmh = driverLocation.SpeedKmh > 0 ? driverLocation.SpeedKmh.Value : AverageSpeedKmh;

        // Calculate ETA in minutes
        var etaMinutes = (int)Math.Ceiling((distanceKm / speedKmh) * 60);

        // Minimum 1 minute if not arrived
        if (etaMinutes < 1 && distanceKm > 0.1)
            etaMinutes = 1;

        return new EtaResult
        {
            EstimatedMinutes = etaMinutes,
            DistanceKm = distanceKm,
            IsEstimate = driverLocation.SpeedKmh == null // Mark as estimate if no actual speed
        };
    }

    private async Task PollLocationAsync(string rideId, int intervalMs, CancellationToken ct)
    {
        bool firstFetch = true;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await GetDriverLocationAsync(rideId);

                if (location != null)
                {
                    LastKnownLocation = location;
                    SetState(TrackingState.Tracking);
                    LocationUpdated?.Invoke(this, location);

                    // Calculate and broadcast ETA
                    var eta = CalculateEta(location, _pickupLatitude, _pickupLongitude);
                    LastKnownEta = eta;
                    EtaUpdated?.Invoke(this, eta);

#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[DriverTrackingService] ETA: {eta.DisplayText}, Distance: {eta.DistanceKm:F2} km");
#endif
                }
                else if (firstFetch)
                {
                    // First fetch failed - set to unavailable but keep trying
                    SetState(TrackingState.Unavailable);
                }
                // If not first fetch and no location, keep showing last known position

                firstFetch = false;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] Poll error: {ex.Message}");
#endif
                if (firstFetch)
                {
                    SetState(TrackingState.Error);
                }
            }

            try
            {
                await Task.Delay(intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[DriverTrackingService] Polling loop ended");
#endif
    }

    private void SetState(TrackingState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            StateChanged?.Invoke(this, newState);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DriverTrackingService] State changed to: {newState}");
#endif
        }
    }

    /// <summary>
    /// Calculates the distance between two GPS coordinates using the Haversine formula.
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public void Dispose()
    {
        StopTracking();
    }
}
