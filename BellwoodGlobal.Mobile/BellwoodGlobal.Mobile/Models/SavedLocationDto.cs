namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// API response DTO for GET/POST/PUT /profile/locations.
/// </summary>
public sealed class SavedLocationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsFavorite { get; set; }
    public int UseCount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }

    public Location ToLocation() => new()
    {
        Id            = Id.ToString(),
        Label         = Label,
        Address       = Address,
        Latitude      = Latitude,
        Longitude     = Longitude,
        IsFavorite    = IsFavorite,
        UseCount      = UseCount,
        LastUpdatedUtc = ModifiedUtc,
        IsVerified    = true   // coordinates came from a verified server record
    };
}
