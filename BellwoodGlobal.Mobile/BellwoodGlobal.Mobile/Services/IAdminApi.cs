using System.Collections.Generic;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;
using BellwoodGlobal.Core.Domain;

namespace BellwoodGlobal.Mobile.Services
{
    public interface IAdminApi
    {
        // ========== QUOTES ==========
        Task SubmitQuoteAsync(QuoteDraft draft);
        Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50);
        Task<QuoteDetail?> GetQuoteAsync(string id);

        // ========== BOOKINGS ==========
        /// <summary>
        /// Submits a new booking request to the Admin API.
        /// </summary>
        Task SubmitBookingAsync(QuoteDraft draft);

        /// <summary>
        /// Retrieves a list of bookings for the current user.
        /// </summary>
        Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(int take = 50);

        /// <summary>
        /// Retrieves detailed information for a specific booking.
        /// </summary>
        Task<Models.BookingDetail?> GetBookingAsync(string id);

        /// <summary>
        /// Cancels a booking request. Only allowed for Requested/Confirmed bookings.
        /// </summary>
        Task CancelBookingAsync(string id);

        // ========== DRIVER TRACKING ==========
        /// <summary>
        /// Gets the current driver location for a ride.
        /// Returns null if no location data is available.
        /// </summary>
        /// <param name="rideId">The ride ID to get driver location for.</param>
        Task<DriverLocation?> GetDriverLocationAsync(string rideId);
    }
}
