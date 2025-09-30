using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public class ProfileService : IProfileService
{
    private readonly Passenger _booker = new()
    {
        FirstName = "Alex",
        LastName = "Morgan",
        PhoneNumber = "312-555-7777",
        EmailAddress = "alex.morgan@example.com"
    };

    private readonly List<Passenger> _passengers = new()
    {
        new Passenger { FirstName = "Taylor", LastName = "Reed", PhoneNumber = "773-555-1122", EmailAddress = "taylor.reed@example.com" },
        new Passenger { FirstName = "Jordan", LastName = "Chen" }
    };

    private readonly List<Models.Location> _locations = new()
    {
        new Models.Location { Label = "Home",   Address = "123 Wacker Dr, Chicago, IL" },
        new Models.Location { Label = "O'Hare", Address = "O'Hare International Airport, Chicago, IL" },
        new Models.Location { Label = "Langham", Address = "330 N Wabash Ave, Chicago, IL" }
    };

    public Passenger GetBooker() => _booker;
    public IReadOnlyList<Passenger> GetSavedPassengers() => _passengers;
    public IReadOnlyList<Models.Location> GetSavedLocations() => _locations;
}
