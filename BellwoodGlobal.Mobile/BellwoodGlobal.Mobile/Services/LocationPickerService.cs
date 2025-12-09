using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Web;
using AppLocation = BellwoodGlobal.Mobile.Models.Location;
using GeoLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Cross-platform location picker service that integrates with native maps apps.
/// Supports Android (Google Maps), iOS (Apple Maps), and Windows (Bing Maps).
/// </summary>
public sealed class LocationPickerService : ILocationPickerService
{
    private readonly IGeolocation _geolocation;
    private readonly IGeocoding _geocoding;
    private readonly ILauncher _launcher;
    private readonly IMap _map;

    // Cache for performance
    private AppLocation? _lastKnownLocation;
    private DateTime _lastLocationTime = DateTime.MinValue;
    private static readonly TimeSpan LocationCacheExpiry = TimeSpan.FromMinutes(2);

    public LocationPickerService()
    {
        _geolocation = Geolocation.Default;
        _geocoding = Geocoding.Default;
        _launcher = Launcher.Default;
        _map = Map.Default;
    }

    /// <inheritdoc />
    public bool IsMapIntegrationAvailable => _map != null;

    /// <inheritdoc />
    public async Task<bool> IsLocationAvailableAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<LocationPickerResult> PickLocationAsync(LocationPickerOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LocationPickerOptions();

        try
        {
            // Step 1: Determine the starting point for the map
            double? startLat = null, startLng = null;
            string? startAddress = null;

            if (options.InitialLocation?.HasCoordinates == true)
            {
                startLat = options.InitialLocation.Latitude;
                startLng = options.InitialLocation.Longitude;
                startAddress = options.InitialLocation.Address;
            }
            else if (!string.IsNullOrWhiteSpace(options.InitialAddress))
            {
                startAddress = options.InitialAddress;
                // Try to geocode the initial address
                var geocoded = await GeocodeAddressAsync(options.InitialAddress, ct);
                if (geocoded?.HasCoordinates == true)
                {
                    startLat = geocoded.Latitude;
                    startLng = geocoded.Longitude;
                }
            }
            else if (options.UseCurrentLocation)
            {
                var current = await GetCurrentLocationAsync(ct);
                if (current?.HasCoordinates == true)
                {
                    startLat = current.Latitude;
                    startLng = current.Longitude;
                }
            }

            // Step 2: Show location input dialog
            var result = await ShowLocationPickerDialogAsync(
                options.Title,
                startAddress,
                startLat,
                startLng,
                options.SuggestedLabel,
                options.AllowSearch);

            if (result is null)
                return LocationPickerResult.Cancelled();

            // Step 3: Geocode if needed
            if (options.GeocodeAddress && !result.HasCoordinates && !string.IsNullOrWhiteSpace(result.Address))
            {
                var geocoded = await GeocodeAddressAsync(result.Address, ct);
                if (geocoded?.HasCoordinates == true)
                {
                    result.Latitude = geocoded.Latitude;
                    result.Longitude = geocoded.Longitude;
                    result.IsVerified = true;
                }
            }

            result.LastUpdatedUtc = DateTime.UtcNow;
            return LocationPickerResult.Succeeded(result);
        }
        catch (PermissionException)
        {
            return LocationPickerResult.Failed("Location permission is required to pick a location. Please enable it in settings.");
        }
        catch (FeatureNotSupportedException)
        {
            return LocationPickerResult.Failed("Location services are not supported on this device.");
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] PickLocationAsync error: {ex}");
#endif
            return LocationPickerResult.Failed($"Failed to pick location: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task OpenInMapsAsync(AppLocation location, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        try
        {
            if (location.HasCoordinates)
            {
                // Open with coordinates
                var mapLocation = new GeoLocation(
                    location.Latitude!.Value,
                    location.Longitude!.Value);

                var options = new MapLaunchOptions
                {
                    Name = string.IsNullOrWhiteSpace(location.Label) ? location.Address : location.Label,
                    NavigationMode = NavigationMode.None
                };

                await _map.OpenAsync(mapLocation, options);
            }
            else if (!string.IsNullOrWhiteSpace(location.Address))
            {
                // Open with address using placemark
                var placemark = new Placemark
                {
                    Thoroughfare = location.Address,
                    Locality = "", // Will be parsed from address
                    AdminArea = "",
                    CountryName = ""
                };

                var options = new MapLaunchOptions
                {
                    Name = string.IsNullOrWhiteSpace(location.Label) ? location.Address : location.Label,
                    NavigationMode = NavigationMode.None
                };

                await _map.OpenAsync(placemark, options);
            }
            else
            {
                throw new ArgumentException("Location must have coordinates or an address.");
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] OpenInMapsAsync error: {ex}");
#endif
            // Fallback: Try to open via URL
            await OpenMapsViaUrlAsync(location);
        }
    }

    /// <inheritdoc />
    public async Task<AppLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        // Check cache first
        if (_lastKnownLocation is not null && DateTime.UtcNow - _lastLocationTime < LocationCacheExpiry)
        {
            return _lastKnownLocation;
        }

        try
        {
            // Request permission if needed
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LocationPickerService] Location permission denied");
#endif
                    return null;
                }
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var geoLocation = await _geolocation.GetLocationAsync(request, ct);

            if (geoLocation == null)
                return null;

            var location = new AppLocation
            {
                Latitude = geoLocation.Latitude,
                Longitude = geoLocation.Longitude,
                Label = "Current Location",
                IsVerified = true,
                LastUpdatedUtc = DateTime.UtcNow
            };

            // Try to reverse geocode for the address
            try
            {
                var placemarks = await _geocoding.GetPlacemarksAsync(geoLocation.Latitude, geoLocation.Longitude);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    location.Address = FormatPlacemarkAddress(placemark);
                }
            }
            catch
            {
                // Reverse geocoding is optional
                location.Address = $"{geoLocation.Latitude:F6}, {geoLocation.Longitude:F6}";
            }

            // Cache the result
            _lastKnownLocation = location;
            _lastLocationTime = DateTime.UtcNow;

            return location;
        }
        catch (PermissionException)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[LocationPickerService] Location permission exception");
#endif
            return null;
        }
        catch (FeatureNotSupportedException)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[LocationPickerService] Location not supported");
#endif
            return null;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] GetCurrentLocationAsync error: {ex}");
#endif
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AppLocation?> GeocodeAddressAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            var locations = await _geocoding.GetLocationsAsync(address);
            var geoLocation = locations?.FirstOrDefault();

            if (geoLocation == null)
                return null;

            return new AppLocation
            {
                Address = address,
                Latitude = geoLocation.Latitude,
                Longitude = geoLocation.Longitude,
                IsVerified = true,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] GeocodeAddressAsync error: {ex}");
#endif
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AppLocation?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var placemarks = await _geocoding.GetPlacemarksAsync(latitude, longitude);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark == null)
                return null;

            return new AppLocation
            {
                Address = FormatPlacemarkAddress(placemark),
                Latitude = latitude,
                Longitude = longitude,
                IsVerified = true,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] ReverseGeocodeAsync error: {ex}");
#endif
            return null;
        }
    }

    /// <inheritdoc />
    public async Task OpenDirectionsAsync(AppLocation? from, AppLocation to, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(to);

        try
        {
            if (to.HasCoordinates)
            {
                var destination = new GeoLocation(
                    to.Latitude!.Value,
                    to.Longitude!.Value);

                var options = new MapLaunchOptions
                {
                    Name = string.IsNullOrWhiteSpace(to.Label) ? to.Address : to.Label,
                    NavigationMode = NavigationMode.Driving
                };

                await _map.OpenAsync(destination, options);
            }
            else if (!string.IsNullOrWhiteSpace(to.Address))
            {
                // Geocode first, then open directions
                var geocoded = await GeocodeAddressAsync(to.Address, ct);
                if (geocoded?.HasCoordinates == true)
                {
                    await OpenDirectionsAsync(from, geocoded, ct);
                }
                else
                {
                    // Fallback to address-based directions via URL
                    await OpenDirectionsViaUrlAsync(from, to);
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] OpenDirectionsAsync error: {ex}");
#endif
            await OpenDirectionsViaUrlAsync(from, to);
        }
    }

    // ========== Private Helper Methods ==========

    private async Task<AppLocation?> ShowLocationPickerDialogAsync(
        string title,
        string? initialAddress,
        double? initialLat,
        double? initialLng,
        string? suggestedLabel,
        bool allowSearch)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
            return null;

        // Show action sheet for location selection method
        var choices = new List<string>();

        if (allowSearch)
            choices.Add("?? Enter Address Manually");

        choices.Add("??? Open Maps App");
        choices.Add("?? Use Current Location");

        if (!string.IsNullOrWhiteSpace(initialAddress))
            choices.Add($"? Use: {TruncateString(initialAddress, 40)}");

        var action = await page.DisplayActionSheet(
            title,
            "Cancel",
            null,
            choices.ToArray());

        if (string.IsNullOrEmpty(action) || action == "Cancel")
            return null;

        if (action == "?? Enter Address Manually")
        {
            return await PromptForManualAddressAsync(page, suggestedLabel);
        }

        if (action == "??? Open Maps App")
        {
            // Open maps app and prompt user to copy/share the address after selection
            await OpenMapsForSelectionAsync(initialLat, initialLng);

            // After user returns, prompt for the address they selected
            return await PromptForManualAddressAsync(page, suggestedLabel,
                "Enter the address from the maps app:");
        }

        if (action == "?? Use Current Location")
        {
            var current = await GetCurrentLocationAsync();
            if (current is not null)
            {
                // Prompt for a label
                var label = await page.DisplayPromptAsync(
                    "Location Label",
                    "Enter a label for this location:",
                    initialValue: suggestedLabel ?? "Current Location",
                    maxLength: 50);

                if (!string.IsNullOrWhiteSpace(label))
                    current.Label = label;

                return current;
            }
            else
            {
                await page.DisplayAlert("Location Unavailable",
                    "Could not get your current location. Please check your location settings and try again.",
                    "OK");
                return null;
            }
        }

        if (action.StartsWith("? Use:"))
        {
            // Use the initial address
            var location = new AppLocation
            {
                Address = initialAddress ?? "",
                Label = suggestedLabel ?? ""
            };

            if (initialLat.HasValue && initialLng.HasValue)
            {
                location.Latitude = initialLat;
                location.Longitude = initialLng;
                location.IsVerified = true;
            }

            // Prompt for label if not set
            if (string.IsNullOrWhiteSpace(location.Label))
            {
                var label = await page.DisplayPromptAsync(
                    "Location Label",
                    "Enter a label for this location:",
                    maxLength: 50);

                if (!string.IsNullOrWhiteSpace(label))
                    location.Label = label;
            }

            return location;
        }

        return null;
    }

    private async Task<AppLocation?> PromptForManualAddressAsync(Page page, string? suggestedLabel, string? prompt = null)
    {
        var address = await page.DisplayPromptAsync(
            "Enter Address",
            prompt ?? "Enter the full address:",
            placeholder: "123 Main St, City, State ZIP",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(address))
            return null;

        var label = await page.DisplayPromptAsync(
            "Location Label",
            "Enter a label for this location (optional):",
            initialValue: suggestedLabel ?? "",
            maxLength: 50);

        return new AppLocation
        {
            Address = address.Trim(),
            Label = label?.Trim() ?? ""
        };
    }

    private async Task OpenMapsForSelectionAsync(double? lat, double? lng)
    {
        try
        {
            if (lat.HasValue && lng.HasValue)
            {
                // Open maps centered on the specified location
                var location = new GeoLocation(lat.Value, lng.Value);
                await _map.OpenAsync(location, new MapLaunchOptions
                {
                    Name = "Select Location",
                    NavigationMode = NavigationMode.None
                });
            }
            else
            {
                // Try to get current location first
                var current = await GetCurrentLocationAsync();
                if (current?.HasCoordinates == true)
                {
                    var location = new GeoLocation(
                        current.Latitude!.Value,
                        current.Longitude!.Value);

                    await _map.OpenAsync(location, new MapLaunchOptions
                    {
                        Name = "Select Location",
                        NavigationMode = NavigationMode.None
                    });
                }
                else
                {
                    // Open maps without a specific location
                    await OpenMapsDefaultAsync();
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LocationPickerService] OpenMapsForSelectionAsync error: {ex}");
#endif
            await OpenMapsDefaultAsync();
        }
    }

    private async Task OpenMapsDefaultAsync()
    {
        // Platform-specific default maps URLs
        string url;

#if ANDROID
        url = "geo:0,0?q=";
#elif IOS || MACCATALYST
        url = "maps://";
#elif WINDOWS
        url = "bingmaps:";
#else
        url = "https://maps.google.com";
#endif

        try
        {
            await _launcher.OpenAsync(url);
        }
        catch
        {
            // Fallback to web maps
            await _launcher.OpenAsync("https://maps.google.com");
        }
    }

    private async Task OpenMapsViaUrlAsync(AppLocation location)
    {
        var query = location.HasCoordinates
            ? $"{location.Latitude},{location.Longitude}"
            : HttpUtility.UrlEncode(location.Address);

        string url;

#if ANDROID
        url = $"geo:0,0?q={query}";
#elif IOS || MACCATALYST
        url = location.HasCoordinates
            ? $"maps://?ll={location.Latitude},{location.Longitude}"
            : $"maps://?q={query}";
#elif WINDOWS
        url = location.HasCoordinates
            ? $"bingmaps:?cp={location.Latitude}~{location.Longitude}"
            : $"bingmaps:?q={query}";
#else
        url = $"https://maps.google.com/maps?q={query}";
#endif

        try
        {
            await _launcher.OpenAsync(url);
        }
        catch
        {
            // Final fallback to Google Maps web
            await _launcher.OpenAsync($"https://maps.google.com/maps?q={query}");
        }
    }

    private async Task OpenDirectionsViaUrlAsync(AppLocation? from, AppLocation to)
    {
        var toQuery = to.HasCoordinates
            ? $"{to.Latitude},{to.Longitude}"
            : HttpUtility.UrlEncode(to.Address);

        var fromQuery = from?.HasCoordinates == true
            ? $"{from.Latitude},{from.Longitude}"
            : from is not null ? HttpUtility.UrlEncode(from.Address) : "";

        string url;

#if ANDROID
        url = string.IsNullOrEmpty(fromQuery)
            ? $"google.navigation:q={toQuery}"
            : $"https://www.google.com/maps/dir/{fromQuery}/{toQuery}";
#elif IOS || MACCATALYST
        url = string.IsNullOrEmpty(fromQuery)
            ? $"maps://?daddr={toQuery}"
            : $"maps://?saddr={fromQuery}&daddr={toQuery}";
#elif WINDOWS
        url = string.IsNullOrEmpty(fromQuery)
            ? $"bingmaps:?rtp=~adr.{toQuery}"
            : $"bingmaps:?rtp=adr.{fromQuery}~adr.{toQuery}";
#else
        url = string.IsNullOrEmpty(fromQuery)
            ? $"https://maps.google.com/maps?daddr={toQuery}"
            : $"https://maps.google.com/maps?saddr={fromQuery}&daddr={toQuery}";
#endif

        try
        {
            await _launcher.OpenAsync(url);
        }
        catch
        {
            // Final fallback
            var webUrl = string.IsNullOrEmpty(fromQuery)
                ? $"https://maps.google.com/maps?daddr={toQuery}"
                : $"https://maps.google.com/maps?saddr={fromQuery}&daddr={toQuery}";
            await _launcher.OpenAsync(webUrl);
        }
    }

    private static string FormatPlacemarkAddress(Placemark placemark)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(placemark.SubThoroughfare))
            parts.Add(placemark.SubThoroughfare);

        if (!string.IsNullOrWhiteSpace(placemark.Thoroughfare))
            parts.Add(placemark.Thoroughfare);

        var streetAddress = string.Join(" ", parts);
        parts.Clear();

        if (!string.IsNullOrWhiteSpace(streetAddress))
            parts.Add(streetAddress);

        if (!string.IsNullOrWhiteSpace(placemark.Locality))
            parts.Add(placemark.Locality);

        if (!string.IsNullOrWhiteSpace(placemark.AdminArea))
            parts.Add(placemark.AdminArea);

        if (!string.IsNullOrWhiteSpace(placemark.PostalCode))
            parts.Add(placemark.PostalCode);

        return string.Join(", ", parts);
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
