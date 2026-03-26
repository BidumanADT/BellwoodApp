using System.Net.Http.Json;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly HttpClient _http;

    // MOCK DATA (remove when LimoAnywhere/Stripe API is ready)
    private static readonly List<PaymentMethod> _mockPaymentMethods = new()
    {
        new PaymentMethod
        {
            Id = "pm_visa4242",
            Last4 = "4242",
            Brand = "Visa",
            ExpiryMonth = 12,
            ExpiryYear = 2025
        },
        new PaymentMethod
        {
            Id = "pm_mc1234",
            Last4 = "1234",
            Brand = "Mastercard",
            ExpiryMonth = 8,
            ExpiryYear = 2026
        }
    };

    public PaymentService(IHttpClientFactory httpFactory)
        => _http = httpFactory.CreateClient("admin");

    public async Task<IReadOnlyList<PaymentMethod>> GetStoredPaymentMethodsAsync()
    {
        // TODO: Replace with real API call once payment endpoints are deployed.
        // The /api/payments/methods endpoint does not exist yet; calling it in
        // Release crashed the app. Use mock data for all configurations until
        // the backend is ready.
        await Task.Delay(300); // Simulate network delay
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PaymentService] Returning {_mockPaymentMethods.Count} mock payment methods");
#endif
        return _mockPaymentMethods;
    }

    public async Task<string> TokenizeCardAsync(string cardNumber, int expiryMonth, int expiryYear, string cvc)
    {
        // TODO: Integrate Stripe SDK for production card tokenization.
        // Until then, return a mock token in all configurations so the
        // booking flow does not crash in Release.
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PaymentService] MOCK Tokenization: Card ending {cardNumber[^4..]}");
#endif
        await Task.Delay(500); // Simulate network delay
        return $"tok_{Guid.NewGuid():N}"; // Mock token
    }

    public async Task<PaymentMethod> SubmitPaymentMethodAsync(NewCardRequest request)
    {
        // TODO: Submit token to real API once payment endpoints are deployed.
        // Until then, create a mock payment method locally in all configurations.
        await Task.Delay(500); // Simulate network delay

        var newMethod = new PaymentMethod
        {
            Id = $"pm_{Guid.NewGuid():N}",
            Last4 = request.Last4,
            Brand = request.Brand,
            ExpiryMonth = DateTime.Now.Month,
            ExpiryYear = DateTime.Now.Year + 3
        };

        _mockPaymentMethods.Add(newMethod);
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PaymentService] Added mock payment method: {newMethod.DisplayName}");
#endif

        return newMethod;
    }
}