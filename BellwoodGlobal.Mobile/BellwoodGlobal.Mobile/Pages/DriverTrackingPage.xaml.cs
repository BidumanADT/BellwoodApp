using System.Text.Json;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Mobile.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using GeoLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace BellwoodGlobal.Mobile.Pages;

/// <summary>
/// Real-time driver tracking page showing the driver's location on a map with ETA.
/// </summary>
public partial class DriverTrackingPage : ContentPage, IQueryAttributable, IDisposable
{
    private readonly IDriverTrackingService _trackingService;
    private Pin? _driverPin;
    private Pin? _pickupPin;

    // Query parameters
    private string? _rideId;
    private double _pickupLatitude;
    private double _pickupLongitude;
    private string? _pickupAddress;

    private bool _isDisposed;

    public DriverTrackingPage()
    {
        InitializeComponent();
        _trackingService = ServiceHelper.GetRequiredService<IDriverTrackingService>();

        // Subscribe to tracking events
        _trackingService.LocationUpdated += OnLocationUpdated;
        _trackingService.StateChanged += OnStateChanged;
        _trackingService.EtaUpdated += OnEtaUpdated;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Expected parameters: rideId, pickupLat, pickupLng, pickupAddress
        if (query.TryGetValue("rideId", out var rideIdObj))
            _rideId = rideIdObj?.ToString();

        if (query.TryGetValue("pickupLat", out var latObj) && double.TryParse(latObj?.ToString(), out var lat))
            _pickupLatitude = lat;

        if (query.TryGetValue("pickupLng", out var lngObj) && double.TryParse(lngObj?.ToString(), out var lng))
            _pickupLongitude = lng;

        if (query.TryGetValue("pickupAddress", out var addrObj))
            _pickupAddress = addrObj?.ToString();

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[DriverTrackingPage] Params: RideId={_rideId}, Pickup=({_pickupLatitude:F6}, {_pickupLongitude:F6}), Address={_pickupAddress}");
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"???????????????????????????????????????????????????");
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] OnAppearing called");
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] RideId: {_rideId ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] Pickup: ({_pickupLatitude:F6}, {_pickupLongitude:F6})");
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] Address: {_pickupAddress ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"???????????????????????????????????????????????????");
#endif

        if (string.IsNullOrWhiteSpace(_rideId))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] !!! RideId is NULL or empty, showing error");
#endif
            await DisplayAlert("Error", "No ride ID provided for tracking.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Set pickup address label
        PickupAddressLabel.Text = string.IsNullOrWhiteSpace(_pickupAddress) ? "Pickup location" : _pickupAddress;

        // Initialize map with pickup location
        InitializeMap();

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] Calling StartTrackingAsync...");
#endif

        // Start tracking
        await _trackingService.StartTrackingAsync(_rideId, _pickupLatitude, _pickupLongitude);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[DriverTrackingPage] StartTrackingAsync completed");
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _trackingService.StopTracking();
    }

    private void InitializeMap()
    {
        // Set initial map region centered on pickup
        if (_pickupLatitude != 0 && _pickupLongitude != 0)
        {
            var pickupLocation = new GeoLocation(_pickupLatitude, _pickupLongitude);

            // Add pickup pin
            _pickupPin = new Pin
            {
                Label = "Pickup",
                Address = _pickupAddress ?? "Your pickup location",
                Type = PinType.Place,
                Location = pickupLocation
            };
            TrackingMap.Pins.Add(_pickupPin);

            // Center map on pickup with reasonable zoom
            TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(pickupLocation, Distance.FromKilometers(2)));
        }
    }

    private void OnLocationUpdated(object? sender, DriverLocation location)
    {
        // Must update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDriverMarker(location);
            UpdateLastUpdatedLabel(location);
            UpdateMapView(location);
        });
    }

    private void OnStateChanged(object? sender, TrackingState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStateUI(state);
        });
    }

    private void OnEtaUpdated(object? sender, EtaResult eta)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateEtaDisplay(eta);
        });
    }

    private void UpdateDriverMarker(DriverLocation location)
    {
        var driverLocation = new GeoLocation(location.Latitude, location.Longitude);

        if (_driverPin == null)
        {
            // Create driver pin
            _driverPin = new Pin
            {
                Label = "Your Driver",
                Address = location.IsStale ? "Last known location" : "Current location",
                Type = PinType.Generic,
                Location = driverLocation
            };
            TrackingMap.Pins.Add(_driverPin);
        }
        else
        {
            // Update existing pin location
            _driverPin.Location = driverLocation;
            _driverPin.Address = location.IsStale ? "Last known location" : "Current location";
        }
    }

    private void UpdateMapView(DriverLocation location)
    {
        // Calculate bounds to show both driver and pickup
        if (_pickupLatitude != 0 && _pickupLongitude != 0)
        {
            var driverLoc = new GeoLocation(location.Latitude, location.Longitude);
            var pickupLoc = new GeoLocation(_pickupLatitude, _pickupLongitude);

            // Calculate center point between driver and pickup
            var centerLat = (location.Latitude + _pickupLatitude) / 2;
            var centerLng = (location.Longitude + _pickupLongitude) / 2;
            var center = new GeoLocation(centerLat, centerLng);

            // Calculate distance to determine zoom level
            var distanceKm = CalculateDistanceKm(location.Latitude, location.Longitude, _pickupLatitude, _pickupLongitude);

            // Add some padding to the view
            var radius = Distance.FromKilometers(Math.Max(distanceKm * 0.7, 0.5));

            TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, radius));
        }
    }

    private void UpdateEtaDisplay(EtaResult eta)
    {
        EtaLabel.Text = eta.DisplayText;

        var distanceText = eta.DistanceKm < 1
            ? $"{(eta.DistanceKm * 1000):F0} meters away"
            : $"{eta.DistanceKm:F1} km away";

        if (eta.IsEstimate)
            distanceText += " (est.)";

        DistanceLabel.Text = distanceText;
    }

    private void UpdateLastUpdatedLabel(DriverLocation location)
    {
        var timeAgo = location.AgeSeconds switch
        {
            < 10 => "Just now",
            < 60 => $"{location.AgeSeconds} seconds ago",
            < 120 => "1 minute ago",
            _ => $"{location.AgeSeconds / 60} minutes ago"
        };

        LastUpdatedLabel.Text = $"Updated {timeAgo}";
        StaleWarningLabel.IsVisible = location.IsStale;
    }

    private void UpdateStateUI(TrackingState state)
    {
        switch (state)
        {
            case TrackingState.Loading:
                LoadingOverlay.IsVisible = true;
                UnavailableOverlay.IsVisible = false;
                LoadingLabel.Text = "Locating your driver...";
                StatusLabel.Text = "Loading";
                StatusFrame.BackgroundColor = Colors.Gray;
                break;

            case TrackingState.Tracking:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = false;
                StatusLabel.Text = "Live";
                StatusFrame.BackgroundColor = TryGetColor("BellwoodGold", Colors.Gold);
                break;

            case TrackingState.NotStarted:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = true;
                UnavailableLabel.Text = "Your driver hasn't started the trip yet.\n\nTracking will begin when your driver is en route.";
                StatusLabel.Text = "Waiting";
                StatusFrame.BackgroundColor = Colors.Orange;
                break;

            case TrackingState.Unavailable:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = true;
                UnavailableLabel.Text = "Driver location temporarily unavailable.\n\nThis can happen due to poor GPS signal or network connectivity.";
                StatusLabel.Text = "Waiting";
                StatusFrame.BackgroundColor = Colors.Orange;
                break;

            case TrackingState.Unauthorized:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = true;
                UnavailableLabel.Text = "You are not authorized to view this ride.\n\nYou can only track your own bookings.";
                StatusLabel.Text = "Error";
                StatusFrame.BackgroundColor = Colors.IndianRed;
                RetryButton.IsVisible = false; // Don't allow retry for auth errors
                break;

            case TrackingState.Error:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = true;
                UnavailableLabel.Text = "Unable to connect to tracking service.\n\nPlease check your internet connection and try again.";
                StatusLabel.Text = "Error";
                StatusFrame.BackgroundColor = Colors.IndianRed;
                RetryButton.IsVisible = true;
                break;

            case TrackingState.Ended:
                LoadingOverlay.IsVisible = false;
                UnavailableOverlay.IsVisible = false;
                StatusLabel.Text = "Ended";
                StatusFrame.BackgroundColor = Colors.Gray;
                EtaLabel.Text = "Ride ended";
                DistanceLabel.Text = "";
                break;
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        _trackingService.StopTracking();
        await Shell.Current.GoToAsync("..");
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_rideId))
        {
            UnavailableOverlay.IsVisible = false;
            LoadingOverlay.IsVisible = true;
            LoadingLabel.Text = "Retrying...";

            // Restart tracking
            await _trackingService.StartTrackingAsync(_rideId, _pickupLatitude, _pickupLongitude);
        }
    }

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

    private static Color TryGetColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _trackingService.LocationUpdated -= OnLocationUpdated;
            _trackingService.StateChanged -= OnStateChanged;
            _trackingService.EtaUpdated -= OnEtaUpdated;
            _isDisposed = true;
        }
    }
}
