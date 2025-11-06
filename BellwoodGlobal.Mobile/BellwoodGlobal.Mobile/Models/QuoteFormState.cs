using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Models
{
    /// <summary>
    /// Legacy alias for TripFormState. Use TripFormState for new code.
    /// This class exists only for backward compatibility with existing QuotePage code.
    /// </summary>
    [System.Obsolete("Use TripFormState instead. QuoteFormState will be removed in a future version.", false)]
    public class QuoteFormState : TripFormState
    {
        // No additional properties – this is a pure alias
    }
}