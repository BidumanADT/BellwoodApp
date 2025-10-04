using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    public interface IQuoteDraftBuilder
    {
        QuoteDraft Build(QuoteFormState state);
    }
}
