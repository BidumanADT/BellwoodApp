using System.Collections.Generic;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services
{
    public interface IAdminApi
    {
        Task SubmitQuoteAsync(QuoteDraft draft);

        Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50);
        Task<QuoteDetail?> GetQuoteAsync(string id);
    }
}
