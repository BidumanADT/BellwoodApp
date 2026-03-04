using System.Net;
using System.Text.Json;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public class ProfileService : IProfileService
{
    private readonly IAdminApi _adminApi;
    private readonly IAuthService _authService;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // ---- booker cache ----
    private Passenger? _cachedBooker;
    private bool _profileLoaded;

    // ---- user identity tracking (detects account switches) ----
    private string? _lastUserId;

    // ---- retry cooldowns ----
    private const int RetryCooldownSeconds = 10;
    private DateTime _lastProfileAttemptUtc = DateTime.MinValue;
    private DateTime _lastPassengersAttemptUtc = DateTime.MinValue;
    private DateTime _lastLocationsAttemptUtc = DateTime.MinValue;

    // ---- in-session lists (populated from API; fallback to Preferences cache) ----
    private readonly List<Passenger> _passengers = new();
    private readonly List<Models.Location> _locations = new();

    // ---- Preferences cache key helpers ----
    private string PassengersCacheKey(string userId) => $"SavedPassengers_{userId}";
    private string LocationsCacheKey(string userId) => $"SavedLocations_{userId}";

    public ProfileService(IAdminApi adminApi, IAuthService authService)
    {
        _adminApi = adminApi;
        _authService = authService;
    }

    // ========== User-switch detection ==========

    /// <summary>
    /// Checks whether the active JWT userId has changed since the last load.
    /// If it has (including null → value or value → different value), resets all cached state
    /// so a new user never sees a previous user's data.
    /// </summary>
    private async Task CheckAndResetForUserAsync()
    {
        var currentUserId = await _authService.GetUserIdFromTokenAsync();
        if (currentUserId != _lastUserId)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] User changed ({_lastUserId} → {currentUserId}). Resetting state.");
#endif
            Reset();
            _lastUserId = currentUserId;
        }
    }

    // ========== Reset ==========

    /// <summary>
    /// Clears all cached profile, passenger, and location state and resets cooldown timers.
    /// Called automatically when a user switch is detected, and explicitly on logout.
    /// </summary>
    public void Reset()
    {
        _profileLoaded = false;
        _cachedBooker = null;
        _passengers.Clear();
        _locations.Clear();
        _lastProfileAttemptUtc = DateTime.MinValue;
        _lastPassengersAttemptUtc = DateTime.MinValue;
        _lastLocationsAttemptUtc = DateTime.MinValue;
        // _lastUserId is NOT cleared here — the caller (CheckAndResetForUserAsync) updates it after Reset().

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[ProfileService] State reset.");
#endif
    }

    // ========== Booker ==========

    public bool IsProfileLoaded => _profileLoaded;

    public async Task LoadProfileAsync()
    {
        // Must run before the _profileLoaded / cooldown guards so a user switch always reloads.
        await CheckAndResetForUserAsync();

        if (_profileLoaded) return;

        if ((DateTime.UtcNow - _lastProfileAttemptUtc).TotalSeconds < RetryCooldownSeconds) return;
        _lastProfileAttemptUtc = DateTime.UtcNow;

        try
        {
            var profile = await _adminApi.GetProfileAsync();

            if (profile is not null)
            {
                _cachedBooker = new Passenger
                {
                    FirstName    = profile.FirstName,
                    LastName     = profile.LastName,
                    PhoneNumber  = profile.PhoneNumber,
                    EmailAddress = profile.EmailAddress
                };
                _profileLoaded = true;

#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[ProfileService] Loaded profile: {_cachedBooker.FirstName} {_cachedBooker.LastName}");
#endif
            }
            else
            {
                // 404 — booker record doesn't exist for this user yet.
                _profileLoaded = true;
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    "[ProfileService] GET /profile returned null (404) — no booker record found.");
#endif
            }
        }
        catch (HttpRequestException ex)
        {
            var isTerminal = ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            if (isTerminal) _profileLoaded = true;

#if DEBUG
            var code = ex.StatusCode.HasValue ? $"HTTP {(int)ex.StatusCode}" : "no status";
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] HTTP error loading profile ({code}): {ex.Message}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] Unexpected error loading profile ({ex.GetType().Name}): {ex.Message}");
#endif
        }
    }

    public Passenger? GetBooker() => _cachedBooker;

    // ========== Saved Passengers ==========

    public IReadOnlyList<Passenger> GetSavedPassengers() => _passengers;

    public async Task LoadSavedPassengersAsync()
    {
        await CheckAndResetForUserAsync();

        if ((DateTime.UtcNow - _lastPassengersAttemptUtc).TotalSeconds < RetryCooldownSeconds) return;
        _lastPassengersAttemptUtc = DateTime.UtcNow;

        try
        {
            var dtos = await _adminApi.GetSavedPassengersAsync();
            _passengers.Clear();
            _passengers.AddRange(dtos.Select(d => d.ToPassenger()));
            await CachePassengersAsync();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Loaded {_passengers.Count} saved passengers from API.");
#endif
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Terminal — don't retry this session
            _lastPassengersAttemptUtc = DateTime.MaxValue;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Terminal error loading passengers: {ex.Message}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] Error loading passengers, falling back to cache ({ex.GetType().Name}): {ex.Message}");
#endif
            if (_passengers.Count == 0)
                await LoadCachedPassengersAsync();
        }
    }

    public async Task<Passenger?> AddSavedPassengerAsync(string firstName, string lastName, string? phoneNumber, string? emailAddress)
    {
        try
        {
            var req = new SavePassengerRequest
            {
                FirstName    = firstName,
                LastName     = lastName,
                PhoneNumber  = NullIfBlank(phoneNumber),
                EmailAddress = NullIfBlank(emailAddress)
            };
            var dto = await _adminApi.CreateSavedPassengerAsync(req);
            if (dto is null) return null;

            var passenger = dto.ToPassenger();
            _passengers.Add(passenger);
            await CachePassengersAsync();
            return passenger;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error adding passenger: {ex.Message}");
#endif
            return null;
        }
    }

    public async Task<bool> UpdateSavedPassengerAsync(string passengerId, string firstName, string lastName, string? phoneNumber, string? emailAddress)
    {
        if (!Guid.TryParse(passengerId, out var guid)) return false;

        try
        {
            var req = new SavePassengerRequest
            {
                FirstName    = firstName,
                LastName     = lastName,
                PhoneNumber  = NullIfBlank(phoneNumber),
                EmailAddress = NullIfBlank(emailAddress)
            };
            await _adminApi.UpdateSavedPassengerAsync(guid, req);

            var existing = _passengers.FirstOrDefault(p => p.Id == passengerId);
            if (existing is not null)
            {
                existing.FirstName    = firstName;
                existing.LastName     = lastName;
                existing.PhoneNumber  = req.PhoneNumber;
                existing.EmailAddress = req.EmailAddress;
            }
            await CachePassengersAsync();
            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error updating passenger {passengerId}: {ex.Message}");
#endif
            return false;
        }
    }

    public async Task<bool> DeleteSavedPassengerAsync(string passengerId)
    {
        if (!Guid.TryParse(passengerId, out var guid)) return false;

        try
        {
            await _adminApi.DeleteSavedPassengerAsync(guid);
            _passengers.RemoveAll(p => p.Id == passengerId);
            await CachePassengersAsync();
            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error deleting passenger {passengerId}: {ex.Message}");
#endif
            return false;
        }
    }

    // ========== Saved Locations ==========

    public IReadOnlyList<Models.Location> GetSavedLocations() => _locations;

    public async Task LoadSavedLocationsAsync()
    {
        await CheckAndResetForUserAsync();

        if ((DateTime.UtcNow - _lastLocationsAttemptUtc).TotalSeconds < RetryCooldownSeconds) return;
        _lastLocationsAttemptUtc = DateTime.UtcNow;

        try
        {
            var dtos = await _adminApi.GetSavedLocationsAsync();
            _locations.Clear();
            _locations.AddRange(dtos.Select(d => d.ToLocation()));
            await CacheLocationsAsync();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Loaded {_locations.Count} saved locations from API.");
#endif
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _lastLocationsAttemptUtc = DateTime.MaxValue;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Terminal error loading locations: {ex.Message}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] Error loading locations, falling back to cache ({ex.GetType().Name}): {ex.Message}");
#endif
            if (_locations.Count == 0)
                await LoadCachedLocationsAsync();
        }
    }

    public async Task<Models.Location?> AddSavedLocationAsync(string label, string address, double latitude, double longitude, bool isFavorite)
    {
        try
        {
            var req = new SaveLocationRequest
            {
                Label      = label,
                Address    = address,
                Latitude   = latitude,
                Longitude  = longitude,
                IsFavorite = isFavorite
            };
            var dto = await _adminApi.CreateSavedLocationAsync(req);
            if (dto is null) return null;

            var location = dto.ToLocation();
            _locations.Add(location);
            await CacheLocationsAsync();
            return location;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error adding location: {ex.Message}");
#endif
            return null;
        }
    }

    public async Task<bool> UpdateSavedLocationAsync(string locationId, string label, string address, double latitude, double longitude, bool isFavorite)
    {
        if (!Guid.TryParse(locationId, out var guid)) return false;

        try
        {
            var req = new SaveLocationRequest
            {
                Label      = label,
                Address    = address,
                Latitude   = latitude,
                Longitude  = longitude,
                IsFavorite = isFavorite
            };
            await _adminApi.UpdateSavedLocationAsync(guid, req);

            var existing = _locations.FirstOrDefault(l => l.Id == locationId);
            if (existing is not null)
            {
                existing.Label      = label;
                existing.Address    = address;
                existing.Latitude   = latitude;
                existing.Longitude  = longitude;
                existing.IsFavorite = isFavorite;
                existing.LastUpdatedUtc = DateTime.UtcNow;
            }
            await CacheLocationsAsync();
            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error updating location {locationId}: {ex.Message}");
#endif
            return false;
        }
    }

    public async Task<bool> DeleteSavedLocationAsync(string locationId)
    {
        if (!Guid.TryParse(locationId, out var guid)) return false;

        try
        {
            await _adminApi.DeleteSavedLocationAsync(guid);
            _locations.RemoveAll(l => l.Id == locationId);
            await CacheLocationsAsync();
            return true;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Error deleting location {locationId}: {ex.Message}");
#endif
            return false;
        }
    }

    public Models.Location? GetLocationById(string locationId)
        => _locations.FirstOrDefault(l => l.Id == locationId);

    public IReadOnlyList<Models.Location> GetFavoriteLocations()
        => _locations
            .Where(l => l.IsFavorite)
            .OrderByDescending(l => l.UseCount)
            .ThenBy(l => l.Label)
            .ToList();

    // ========== Cache Helpers ==========

    private async Task CachePassengersAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdFromTokenAsync() ?? "anon";
            var json = JsonSerializer.Serialize(_passengers, _json);
            Preferences.Set(PassengersCacheKey(userId), json);
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to cache passengers: {ex.Message}");
#endif
        }
    }

    private async Task CacheLocationsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdFromTokenAsync() ?? "anon";
            var json = JsonSerializer.Serialize(_locations, _json);
            Preferences.Set(LocationsCacheKey(userId), json);
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to cache locations: {ex.Message}");
#endif
        }
    }

    private async Task LoadCachedPassengersAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdFromTokenAsync() ?? "anon";
            var json = Preferences.Get(PassengersCacheKey(userId), null as string);
            if (string.IsNullOrEmpty(json)) return;

            var cached = JsonSerializer.Deserialize<List<Passenger>>(json, _json);
            if (cached is null) return;

            _passengers.Clear();
            _passengers.AddRange(cached);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Loaded {_passengers.Count} passengers from Preferences cache.");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to load cached passengers: {ex.Message}");
#endif
        }
    }

    private async Task LoadCachedLocationsAsync()
    {
        try
        {
            var userId = await _authService.GetUserIdFromTokenAsync() ?? "anon";
            var json = Preferences.Get(LocationsCacheKey(userId), null as string);
            if (string.IsNullOrEmpty(json)) return;

            var cached = JsonSerializer.Deserialize<List<Models.Location>>(json, _json);
            if (cached is null) return;

            _locations.Clear();
            _locations.AddRange(cached);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Loaded {_locations.Count} locations from Preferences cache.");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to load cached locations: {ex.Message}");
#endif
        }
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
