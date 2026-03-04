using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// API response DTO for GET/POST/PUT /profile/passengers.
/// </summary>
public sealed class SavedPassengerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }

    public Passenger ToPassenger() => new()
    {
        Id           = Id.ToString(),
        FirstName    = FirstName,
        LastName     = LastName,
        PhoneNumber  = PhoneNumber,
        EmailAddress = EmailAddress
    };
}
