namespace BellwoodGlobal.Mobile.Models;

public class Location
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "";
    public string Address { get; set; } = "";

    public override string ToString()
        => string.IsNullOrWhiteSpace(Label) ? Address : $"{Label} - {Address}";
}
