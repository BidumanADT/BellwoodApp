using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class AdminApi : IAdminApi
    {
        private readonly HttpClient _http;

        public AdminApi(IHttpClientFactory httpFactory)
            => _http = httpFactory.CreateClient("admin");

        public async Task SubmitQuoteAsync(QuoteDraft draft)
        {
            using var res = await _http.PostAsJsonAsync("/quotes", draft);
            res.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50)
        {
            var items = await _http.GetFromJsonAsync<List<QuoteListItem>>($"/quotes/list?take={take}")
                        ?? new List<QuoteListItem>();
            return items;
        }

        public async Task<QuoteDetail?> GetQuoteAsync(string id)
            => await _http.GetFromJsonAsync<QuoteDetail>($"/quotes/{id}");
    }
}
