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

        /// <summary>
        /// Accepts a quote and converts it to a booking.
        /// Only allowed for quotes in "Responded" status owned by the current user.
        /// </summary>
        Task<AcceptQuoteResponse> AcceptQuoteAsync(string quoteId);

        /// <summary>
        /// Cancels a quote request.
        /// Only allowed for quotes in Pending/Acknowledged/Responded status.
        /// </summary>
        Task CancelQuoteAsync(string quoteId);

        // ========== BOOKINGS ==========
        Task SubmitBookingAsync(QuoteDraft draft);
        Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(int take = 50);
        Task<Models.BookingDetail?> GetBookingAsync(string id);
        Task CancelBookingAsync(string id);

        // ========== DRIVER TRACKING ==========
        Task<DriverLocation?> GetDriverLocationAsync(string rideId);

        // ========== BOOKER PROFILE ==========
        /// <summary>
        /// Fetches the authenticated booker's profile from GET /profile.
        /// Returns null if the profile does not exist (404) or the request fails.
        /// </summary>
        Task<BookerProfile?> GetProfileAsync();

        /// <summary>
        /// Backward-compatible alias for GetProfileAsync().
        /// </summary>
        Task<BookerProfile?> GetBookerProfileAsync();

        /// <summary>
        /// Updates the authenticated booker's profile via PUT /profile.
        /// </summary>
        Task UpdateProfileAsync(string firstName, string lastName, string? phoneNumber, string? emailAddress);

        // ========== SAVED PASSENGERS ==========
        /// <summary>
        /// Returns the caller's saved passengers from GET /profile/passengers.
        /// Always returns a list; never 404.
        /// </summary>
        Task<List<SavedPassengerDto>> GetSavedPassengersAsync();

        /// <summary>
        /// Creates a saved passenger via POST /profile/passengers. Returns the created record (201).
        /// </summary>
        Task<SavedPassengerDto?> CreateSavedPassengerAsync(SavePassengerRequest request);

        /// <summary>
        /// Updates a saved passenger via PUT /profile/passengers/{id}.
        /// </summary>
        Task UpdateSavedPassengerAsync(Guid id, SavePassengerRequest request);

        /// <summary>
        /// Deletes a saved passenger via DELETE /profile/passengers/{id}.
        /// </summary>
        Task DeleteSavedPassengerAsync(Guid id);

        // ========== SAVED LOCATIONS ==========
        /// <summary>
        /// Returns the caller's saved locations from GET /profile/locations.
        /// Ordered by favorite status, then descending useCount, then most recently modified.
        /// </summary>
        Task<List<SavedLocationDto>> GetSavedLocationsAsync();

        /// <summary>
        /// Creates a saved location via POST /profile/locations. Returns the created record (201).
        /// </summary>
        Task<SavedLocationDto?> CreateSavedLocationAsync(SaveLocationRequest request);

        /// <summary>
        /// Updates a saved location via PUT /profile/locations/{id}.
        /// The server preserves useCount; clients cannot override it.
        /// </summary>
        Task UpdateSavedLocationAsync(Guid id, SaveLocationRequest request);

        /// <summary>
        /// Deletes a saved location via DELETE /profile/locations/{id}.
        /// </summary>
        Task DeleteSavedLocationAsync(Guid id);
    }
}
