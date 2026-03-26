using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    /// <summary>
    /// Service for persisting and restoring form state across app lifecycle events.
    /// Uses MAUI Preferences for platform-agnostic persistence.
    /// </summary>
    public interface IFormStateService
    {
        // ===== Quote Form =====
        
        /// <summary>
        /// Saves the current Quote form UI state to persistent storage.
        /// </summary>
        Task SaveQuoteFormStateAsync(QuotePageState state);
        
        /// <summary>
        /// Loads the saved Quote form UI state, or null if none exists.
        /// </summary>
        Task<QuotePageState?> LoadQuoteFormStateAsync();
        
        /// <summary>
        /// Clears any saved Quote form state.
        /// </summary>
        Task ClearQuoteFormStateAsync();
        
        /// <summary>
        /// Checks if a saved Quote form state exists without loading it.
        /// </summary>
        bool HasSavedQuoteForm();
        
        // ===== Booking Form =====
        
        /// <summary>
        /// Saves the current Booking form UI state to persistent storage.
        /// </summary>
        Task SaveBookingFormStateAsync(BookRidePageState state);
        
        /// <summary>
        /// Loads the saved Booking form UI state, or null if none exists.
        /// </summary>
        Task<BookRidePageState?> LoadBookingFormStateAsync();
        
        /// <summary>
        /// Clears any saved Booking form state.
        /// </summary>
        Task ClearBookingFormStateAsync();
        
        /// <summary>
        /// Checks if a saved Booking form state exists without loading it.
        /// </summary>
        bool HasSavedBookingForm();
    }
}
