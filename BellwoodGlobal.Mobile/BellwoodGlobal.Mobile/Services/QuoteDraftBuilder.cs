using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Services
{
    /// <summary>
    /// Legacy alias for TripDraftBuilder. Redirects to shared implementation.
    /// </summary>
    [System.Obsolete("Use ITripDraftBuilder/TripDraftBuilder instead.", false)]
    public sealed class QuoteDraftBuilder : IQuoteDraftBuilder
    {
        private readonly ITripDraftBuilder _tripBuilder = new TripDraftBuilder();

        public QuoteDraft Build(QuoteFormState state)
        {
            return _tripBuilder.Build((TripFormState)state);
        }
    }
}
