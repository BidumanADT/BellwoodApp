using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;


namespace BellwoodGlobal.Mobile.Services
{
    public interface IQuoteDraftBuilder
    {
        QuoteDraft Build(QuoteFormState state);
    }
}
