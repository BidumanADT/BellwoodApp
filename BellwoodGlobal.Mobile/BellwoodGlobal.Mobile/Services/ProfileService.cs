using System.Net;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public class ProfileService : IProfileService
{
    private readonly IAdminApi _adminApi;

    private Passenger? _cachedBooker;
    private bool _profileLoaded;

    // Retry cooldown — prevents hammering the API on every page appearance after a transient failure.
    private const int RetryCooldownSeconds = 10;
    private DateTime _lastAttemptUtc = DateTime.MinValue;

    // In-session lists — empty by default until the user adds entries.
    // Saved-passenger and saved-location persistence will be added when
    // the corresponding AdminAPI endpoints are available.
    private readonly List<Passenger> _passengers = new();
    private readonly List<Models.Location> _locations = new();

    public ProfileService(IAdminApi adminApi)
    {
        _adminApi = adminApi;
    }

    // ========== Booker ==========

    public bool IsProfileLoaded => _profileLoaded;

    public async Task LoadProfileAsync()
    {
        // Already successfully loaded — fully idempotent, no retry needed.
        if (_profileLoaded) return;

        // Cooldown: don't spam the API on every page appearance after a transient failure.
        if ((DateTime.UtcNow - _lastAttemptUtc).TotalSeconds < RetryCooldownSeconds) return;

        _lastAttemptUtc = DateTime.UtcNow;

        try
        {
            var profile = await _adminApi.GetBookerProfileAsync();

            if (profile is not null)
            {
                _cachedBooker = new Passenger
                {
                    FirstName    = profile.FirstName,
                    LastName     = profile.LastName,
                    PhoneNumber  = profile.PhoneNumber,
                    EmailAddress = profile.EmailAddress
                };

                // Only flag as loaded on genuine success — allows retry on any prior failure.
                _profileLoaded = true;

#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[ProfileService] Loaded: {_cachedBooker.FirstName} {_cachedBooker.LastName} <{_cachedBooker.EmailAddress}>");
#endif
            }
            else
            {
                // API returned 200 with no body — booker record doesn't exist for this user.
                // Treat as terminal for this session: show the incomplete label, don't retry.
                _profileLoaded = true;
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    "[ProfileService] /api/bookers/me returned null — no booker record found. Showing incomplete label.");
#endif
            }
        }
        catch (HttpRequestException ex)
        {
            var isTerminal = ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

            if (isTerminal)
            {
                // 401/403 won't resolve without re-auth or a role change — stop retrying this session.
                _profileLoaded = true;
            }
            // else: network/5xx — _profileLoaded stays false, cooldown governs next retry.

#if DEBUG
            var code     = ex.StatusCode.HasValue ? $"HTTP {(int)ex.StatusCode}" : "no status";
            var category = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "token/auth issue — check JWT expiry",
                HttpStatusCode.Forbidden    => "role/policy issue — check BookerOrStaff claim",
                _                           => "network or server error (will retry)"
            };
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] HTTP error loading profile ({code}, {category}): {ex.Message}");
#endif
        }
        catch (Exception ex)
        {
            // Unexpected (e.g. JSON deserialization). _profileLoaded stays false; will retry after cooldown.
#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[ProfileService] Unexpected error loading profile ({ex.GetType().Name}): {ex.Message}");
#endif
        }
    }

    public Passenger? GetBooker() => _cachedBooker;

    // ========== Passengers ==========

    public IReadOnlyList<Passenger> GetSavedPassengers() => _passengers;

    public void AddPassenger(Passenger passenger)
    {
        ArgumentNullException.ThrowIfNull(passenger);
        _passengers.Add(passenger);
    }

    public bool RemovePassenger(string passengerId)
    {
        var passenger = _passengers.FirstOrDefault(p => p.Id == passengerId);
        if (passenger != null)
        {
            _passengers.Remove(passenger);
            return true;
        }
        return false;
    }

    // ========== Locations ==========

    public IReadOnlyList<Models.Location> GetSavedLocations() => _locations;

    public void AddLocation(Models.Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        location.LastUpdatedUtc ??= DateTime.UtcNow;
        _locations.Add(location);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[ProfileService] Added location: {location.Label} - {location.Address}");
        if (location.HasCoordinates)
            System.Diagnostics.Debug.WriteLine($"[ProfileService]   Coordinates: {location.Latitude}, {location.Longitude}");
#endif
    }

    public bool UpdateLocation(Models.Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        var existing = _locations.FirstOrDefault(l => l.Id == location.Id);
        if (existing == null)
            return false;

        var index = _locations.IndexOf(existing);
        location.LastUpdatedUtc = DateTime.UtcNow;
        _locations[index] = location;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[ProfileService] Updated location: {location.Label} - {location.Address}");
#endif

        return true;
    }

    public bool RemoveLocation(string locationId)
    {
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (location != null)
        {
            _locations.Remove(location);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Removed location: {location.Label}");
#endif

            return true;
        }
        return false;
    }

    public Models.Location? GetLocationById(string locationId)
        => _locations.FirstOrDefault(l => l.Id == locationId);

    public IReadOnlyList<Models.Location> GetFavoriteLocations()
        => _locations
            .Where(l => l.IsFavorite)
            .OrderByDescending(l => l.UseCount)
            .ThenBy(l => l.Label)
            .ToList();

    public void SetLocationFavorite(string locationId, bool isFavorite)
    {
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (location != null)
        {
            location.IsFavorite = isFavorite;
            location.LastUpdatedUtc = DateTime.UtcNow;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Location '{location.Label}' favorite: {isFavorite}");
#endif
        }
    }
}
