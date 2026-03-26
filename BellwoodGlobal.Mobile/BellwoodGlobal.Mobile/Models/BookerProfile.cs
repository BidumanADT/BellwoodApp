namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Response DTO for GET /api/bookers/me — the authenticated booker's profile.
/// </summary>
public sealed class BookerProfile
{
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public string? DisplayName { get; set; }
}
