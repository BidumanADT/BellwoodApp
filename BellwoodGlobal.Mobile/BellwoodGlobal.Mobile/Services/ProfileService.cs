using System.Text.Json;
using BellwoodGlobal.Mobile.Models;
using Microsoft.Maui.Storage;

namespace BellwoodGlobal.Mobile.Services;

public class ProfileService : IProfileService
{
    private const string KeyPassengers = "saved_passengers_v1";
    private const string KeyLocations = "saved_locations_v1";

    // --- Booker (unchanged) ---
    private readonly Passenger _booker = new()
    {
        FirstName = "Alice",
        LastName = "Morgan",
        PhoneNumber = "312-555-7777",
        EmailAddress = "alice.morgan@example.com"
    };

    // --- In-memory caches loaded/saved to Preferences ---
    private List<Passenger> _passengers;
    private List<Models.Location> _locations;

    public ProfileService()
    {
        // Load existing lists, or seed your defaults on first run
        _passengers = LoadList<Passenger>(KeyPassengers) ?? new();
        _locations = LoadList<Models.Location>(KeyLocations) ?? new();

        if (_passengers.Count == 0)
        {
            _passengers = new()
            {
                new Passenger { FirstName = "Taylor", LastName = "Reed", PhoneNumber = "773-555-1122", EmailAddress = "taylor.reed@example.com" },
                new Passenger { FirstName = "Jordan", LastName = "Chen" }
            };
            SaveList(KeyPassengers, _passengers);
        }

        if (_locations.Count == 0)
        {
            _locations = new()
            {
                new Models.Location { Label = "Home",   Address = "123 Wacker Dr, Chicago, IL" },
                new Models.Location { Label = "O'Hare", Address = "O'Hare International Airport, Chicago, IL" },
                new Models.Location { Label = "Langham", Address = "330 N Wabash Ave, Chicago, IL" },
                new Models.Location { Label = "Signature FBO (ORD)", Address = "825 Patton Drive, Chicago, IL 60666" }
            };
            SaveList(KeyLocations, _locations);
        }
    }

    public Passenger GetBooker() => _booker;

    public IReadOnlyList<Passenger> GetSavedPassengers() => _passengers;
    public IReadOnlyList<Models.Location> GetSavedLocations() => _locations;

    public bool AddPassenger(Passenger p)
    {
        if (p is null) return false;

        // De-dupe by First+Last+Email+Phone (all case-insensitive, blanks allowed)
        bool exists = _passengers.Any(x =>
            string.Equals(x.FirstName?.Trim(), p.FirstName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.LastName?.Trim(), p.LastName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals((x.EmailAddress ?? "").Trim(), (p.EmailAddress ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals((x.PhoneNumber ?? "").Trim(), (p.PhoneNumber ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

        if (exists) return false;

        _passengers.Add(p);
        SaveList(KeyPassengers, _passengers);
        return true;
    }

    public bool AddLocation(Models.Location l)
    {
        if (l is null) return false;

        // De-dupe by Label+Address
        bool exists = _locations.Any(x =>
            string.Equals(x.Label?.Trim(), l.Label?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Address?.Trim(), l.Address?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (exists) return false;

        _locations.Add(l);
        SaveList(KeyLocations, _locations);
        return true;
    }

    // ---- Persistence helpers ----
    private static List<T>? LoadList<T>(string key)
    {
        var json = Preferences.Get(key, "");
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<T>>(json); }
        catch { return null; }
    }

    private static void SaveList<T>(string key, List<T> list)
        => Preferences.Set(key, JsonSerializer.Serialize(list));
}
