using BellwoodGlobal.Mobile.Models;
using System.Net;
using System.Net.Http.Json;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services;

public class RideService : IRideService
{
    private readonly HttpClient _client;

    public RideService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("rides");
    }

    public async Task<IReadOnlyList<Ride>> GetHistoryAsync(CancellationToken ct = default)
    {
        foreach (var path in new[] { "/api/rides/history", "/api/rides" })
        {
            var res = await _client.GetAsync(path, ct);
            if (res.StatusCode == HttpStatusCode.NotFound) continue;
            res.EnsureSuccessStatusCode();

            var data = await res.Content.ReadFromJsonAsync<List<Ride>>(cancellationToken: ct)
                       ?? new List<Ride>();

            // Future filter for when list includes future rides:
            // data = data.Where(r => string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();

            return data.OrderByDescending(r => r.PickupTime).ToList();
        }

        throw new HttpRequestException("No matching rides endpoint found (tried /api/rides/history, /api/rides).");
    }
}
