namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Request payload for tokenizing a new card.
/// Contains NON-sensitive metadata only (name, ZIP).
/// Actual card number is sent directly to Stripe SDK.
/// </summary>
public sealed class NewCardRequest
{
    public string NameOnCard { get; set; } = "";
    public string BillingZip { get; set; } = "";
    public string StripeToken { get; set; } = ""; // Populated by Stripe SDK
    public string Last4 { get; set; } = ""; 
    public string Brand { get; set; } = "Visa";
}