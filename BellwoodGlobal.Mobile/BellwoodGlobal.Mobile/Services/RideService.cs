using System.Net;
using System.Net.Http.Json;
using BellwoodGlobal.Core.Domain;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public class RideService : IRideService
{
    private readonly HttpClient _client;

    public RideService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("rides");
    }

    // Existing: pulls past rides (kept as-is, with your fallback)
    public async Task<IReadOnlyList<Ride>> GetHistoryAsync(CancellationToken ct = default)
    {
        foreach (var path in new[] { "/api/rides/history", "/api/rides" })
        {
            var res = await _client.GetAsync(path, ct);
            if (res.StatusCode == HttpStatusCode.NotFound) continue;
            res.EnsureSuccessStatusCode();

            var data = await res.Content.ReadFromJsonAsync<List<Ride>>(cancellationToken: ct)
                       ?? new List<Ride>();

            // If the combined list ever includes future rides, you can filter here.
            return data.OrderByDescending(r => r.PickupTime).ToList();
        }

        throw new HttpRequestException("No matching rides endpoint found (tried /api/rides/history, /api/rides).");
    }

    // NEW: create a booking (POST /api/rides)
    public async Task<string> CreateAsync(BookRideRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.PickupAddress) ||
            string.IsNullOrWhiteSpace(request.DropoffAddress))
            throw new ArgumentException("Pickup and dropoff addresses are required.");

        var res = await _client.PostAsJsonAsync("/api/rides", request);
        res.EnsureSuccessStatusCode();

        var created = await res.Content.ReadFromJsonAsync<RideListItem>();
        if (created == null || string.IsNullOrWhiteSpace(created.Id))
            throw new HttpRequestException("Ride created but no Id returned from server.");

        return created.Id;
    }

    // NEW: list bookings (GET /api/rides)
    public async Task<IReadOnlyList<RideListItem>> ListAsync()
    {
        var res = await _client.GetAsync("/api/rides");
        res.EnsureSuccessStatusCode();

        var items = await res.Content.ReadFromJsonAsync<List<RideListItem>>() ?? new List<RideListItem>();
        return items.OrderByDescending(r => r.PickupTime).ToList();
    }
}
