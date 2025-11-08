using System.Net.Http.Json;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly HttpClient _http;

    // MOCK DATA (remove when LimoAnywhere API is ready)
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
#if DEBUG
        // Return mock data in DEBUG mode (no API call)
        await Task.Delay(300); // Simulate network delay
        System.Diagnostics.Debug.WriteLine($"[PaymentService] Returning {_mockPaymentMethods.Count} mock payment methods");
        return _mockPaymentMethods;
#else
        // Production: Call real API
        var methods = await _http.GetFromJsonAsync<List<PaymentMethod>>("/api/payments/methods");
        return methods ?? new List<PaymentMethod>();
#endif
    }

    public async Task<string> TokenizeCardAsync(string cardNumber, int expiryMonth, int expiryYear, string cvc)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PaymentService] MOCK Tokenization: Card ending {cardNumber[^4..]}");
        await Task.Delay(500); // Simulate network delay
        return $"tok_{Guid.NewGuid():N}"; // Mock token
#else
        throw new NotImplementedException("Stripe SDK integration required for production");
#endif
    }

    public async Task<PaymentMethod> SubmitPaymentMethodAsync(NewCardRequest request)
    {
#if DEBUG
        // MOCK: Generate a new payment method locally
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
        System.Diagnostics.Debug.WriteLine($"[PaymentService] Added mock payment method: {newMethod.DisplayName}");

        return newMethod;
#else
        // Production: Call real API
        var response = await _http.PostAsJsonAsync("/api/payments/methods", request);
        response.EnsureSuccessStatusCode();
        var newMethod = await response.Content.ReadFromJsonAsync<PaymentMethod>();
        return newMethod ?? throw new InvalidOperationException("Failed to add payment method");
#endif
    }
}