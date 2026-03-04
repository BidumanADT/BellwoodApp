namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Request body for POST/PUT /profile/locations.
/// </summary>
public sealed class SaveLocationRequest
{
    public string Label { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsFavorite { get; set; }
}
