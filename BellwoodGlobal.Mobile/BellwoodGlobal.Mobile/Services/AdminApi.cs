using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class AdminApi : IAdminApi
    {
        private readonly HttpClient _http;
        public AdminApi(HttpClient http) => _http = http;

        public async Task SubmitQuoteAsync(QuoteDraft draft)
        {
            var resp = await _http.PostAsJsonAsync("/quotes", draft);
            resp.EnsureSuccessStatusCode();
        }
    }
}
