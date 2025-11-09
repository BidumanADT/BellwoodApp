using System.Net.Http.Json;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public interface IPaymentService
{
    /// <summary>
    /// Fetches stored payment methods for the current user.
    /// Returns only last 4 digits + metadata (secure).
    /// </summary>
    Task<IReadOnlyList<PaymentMethod>> GetStoredPaymentMethodsAsync();

    /// <summary>
    /// Tokenizes a new card using Stripe SDK (secure, direct to Stripe).
    /// Returns a one-time token (never stores raw card data).
    /// </summary>
    /// <param name="cardNumber">Full card number (transmitted directly to Stripe)</param>
    /// <param name="expiryMonth">Expiration month (1-12)</param>
    /// <param name="expiryYear">Expiration year (e.g., 2025)</param>
    /// <param name="cvc">CVC code (3-4 digits)</param>
    Task<string> TokenizeCardAsync(string cardNumber, int expiryMonth, int expiryYear, string cvc);

    /// <summary>
    /// Submits a new payment method (via token) to AdminAPI/LimoAnywhere.
    /// Returns the new payment_method_id for future bookings.
    /// </summary>
    Task<PaymentMethod> SubmitPaymentMethodAsync(NewCardRequest request);
}