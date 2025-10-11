using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    public interface IAdminApi
    {
        Task SubmitQuoteAsync(QuoteDraft draft);
    }
}
