using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public class ProfileService : IProfileService
{
    private readonly Passenger _booker = new()
    {
        FirstName = "Alice",
        LastName = "Morgan",
        PhoneNumber = "312-555-7777",
        EmailAddress = "alice.morgan@example.com"
    };

    private readonly List<Passenger> _passengers = new()
    {
        new Passenger { FirstName = "Taylor", LastName = "Reed", PhoneNumber = "773-555-1122", EmailAddress = "taylor.reed@example.com" },
        new Passenger { FirstName = "Jordan", LastName = "Chen" }
    };

    private readonly List<Models.Location> _locations = new()
    {
        new Models.Location 
        { 
            Label = "Home", 
            Address = "123 Wacker Dr, Chicago, IL",
            IsFavorite = true,
            Latitude = 41.8864,
            Longitude = -87.6365,
            IsVerified = true
        },
        new Models.Location 
        { 
            Label = "O'Hare", 
            Address = "O'Hare International Airport, Chicago, IL",
            IsFavorite = true,
            Latitude = 41.9742,
            Longitude = -87.9073,
            IsVerified = true
        },
        new Models.Location 
        { 
            Label = "Langham", 
            Address = "330 N Wabash Ave, Chicago, IL",
            Latitude = 41.8887,
            Longitude = -87.6268,
            IsVerified = true
        },
        new Models.Location 
        { 
            Label = "Signature FBO (ORD)", 
            Address = "825 Patton Drive, Chicago, IL 60666",
            Latitude = 41.9853,
            Longitude = -87.8808,
            IsVerified = true
        }
    };

    // ========== Booker ==========
    public Passenger GetBooker() => _booker;

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
        
        // Set timestamp if not already set
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
    {
        return _locations.FirstOrDefault(l => l.Id == locationId);
    }

    public IReadOnlyList<Models.Location> GetFavoriteLocations()
    {
        return _locations
            .Where(l => l.IsFavorite)
            .OrderByDescending(l => l.UseCount)
            .ThenBy(l => l.Label)
            .ToList();
    }

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
