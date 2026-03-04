namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Request body for POST/PUT /profile/passengers.
/// </summary>
public sealed class SavePassengerRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
}
