namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Represents a stored payment method (card) for the customer.
/// Only contains non-sensitive data (last 4 digits, brand).
/// </summary>
public sealed class PaymentMethod
{
    public string Id { get; set; } = ""; // e.g., "pm_abc123"
    public string Last4 { get; set; } = ""; // e.g., "4242"
    public string Brand { get; set; } = "Visa"; // Visa, Mastercard, Amex, Discover
    public int ExpiryMonth { get; set; } // 1-12
    public int ExpiryYear { get; set; } // e.g., 2025

    /// <summary>
    /// Display string for picker: "Visa ••4242 (Exp 12/25)"
    /// </summary>
    public string DisplayName => $"{Brand} ••{Last4} (Exp {ExpiryMonth:D2}/{ExpiryYear % 100:D2})";

    public override string ToString() => DisplayName;
}