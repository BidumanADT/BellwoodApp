using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Services
{
    /// <summary>
    /// Builds a QuoteDraft from trip form state (used by both Quote and Booking flows).
    /// </summary>
    public interface ITripDraftBuilder
    {
        QuoteDraft Build(TripFormState state);
    }
}