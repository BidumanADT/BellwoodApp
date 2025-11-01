using BellwoodGlobal.Mobile.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class AdminApi : IAdminApi
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

        public AdminApi(IHttpClientFactory httpFactory)
            => _http = httpFactory.CreateClient("admin");

        public async Task SubmitQuoteAsync(QuoteDraft draft)
        {
            using var res = await _http.PostAsJsonAsync("/quotes", draft);
            res.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50)
        {
            var json = await _http.GetStringAsync($"/quotes/list?take={take}");
            var items = JsonSerializer.Deserialize<List<QuoteListItem>>(json, _json)
                        ?? new List<QuoteListItem>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] /quotes/list -> {items.Count} items");
            if (items.Count > 0)
            {
                var first = items[0];
                System.Diagnostics.Debug.WriteLine(
                    $"[AdminApi] First: Id={first.Id}, Passenger={first.PassengerName}, Pickup={first.PickupLocation}, Created={first.CreatedUtc:o}");
            }
#endif
            return items;
        }

        public async Task<QuoteDetail?> GetQuoteAsync(string id)
            => await _http.GetFromJsonAsync<QuoteDetail>($"/quotes/{id}", _json);
    }
}
