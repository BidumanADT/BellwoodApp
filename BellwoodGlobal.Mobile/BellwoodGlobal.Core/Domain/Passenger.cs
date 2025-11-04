namespace BellwoodGlobal.Core.Domain;

public class Passenger
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }

    public override string ToString() => $"{FirstName} {LastName}".Trim();
}
