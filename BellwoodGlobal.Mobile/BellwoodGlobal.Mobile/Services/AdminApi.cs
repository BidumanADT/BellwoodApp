using BellwoodGlobal.Mobile.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using BellwoodGlobal.Core.Domain;
using System.Net;

namespace BellwoodGlobal.Mobile.Services
{
    public sealed class AdminApi : IAdminApi
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

        public AdminApi(IHttpClientFactory httpFactory)
            => _http = httpFactory.CreateClient("admin");

        // ========== QUOTES ==========
        public async Task SubmitQuoteAsync(QuoteDraft draft)
        {
            using var res = await _http.PostAsJsonAsync("/quotes", draft);
            res.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<QuoteListItem>> GetQuotesAsync(int take = 50)
        {
            var json = await _http.GetStringAsync($"/quotes/list?take={take}");
            var items = JsonSerializer.Deserialize<List<QuoteListItem>>(json, _json)
                        ?? new List<QuoteListItem>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] /quotes/list -> {items.Count} items");
            if (items.Count > 0)
            {
                var first = items[0];
                System.Diagnostics.Debug.WriteLine(
                    $"[AdminApi] First: Id={first.Id}, Passenger={first.PassengerName}, Pickup={first.PickupLocation}, Created={first.CreatedUtc:o}");
            }
#endif
            return items;
        }

        public async Task<QuoteDetail?> GetQuoteAsync(string id)
            => await _http.GetFromJsonAsync<QuoteDetail>($"/quotes/{id}", _json);

        // ========== BOOKINGS ==========
        public async Task SubmitBookingAsync(QuoteDraft draft)
        {
            using var res = await _http.PostAsJsonAsync("/bookings", draft);
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Booking submitted: Pickup={draft.PickupDateTime:g}, Vehicle={draft.VehicleClass}");
#endif
        }

        public async Task<IReadOnlyList<BookingListItem>> GetBookingsAsync(int take = 50)
        {
            var json = await _http.GetStringAsync($"/bookings/list?take={take}");
            var items = JsonSerializer.Deserialize<List<BookingListItem>>(json, _json)
                        ?? new List<BookingListItem>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] /bookings/list -> {items.Count} items");
            if (items.Count > 0)
            {
                var first = items[0];
                System.Diagnostics.Debug.WriteLine(
                    $"[AdminApi] First: Id={first.Id}, Passenger={first.PassengerName}, Pickup={first.PickupLocation}, Status={first.Status}");
            }
#endif
            return items;
        }

        public async Task<Models.BookingDetail?> GetBookingAsync(string id)
            => await _http.GetFromJsonAsync<Models.BookingDetail>($"/bookings/{id}", _json);

        public async Task CancelBookingAsync(string id)
        {
            using var res = await _http.PostAsync($"/bookings/{id}/cancel", null);
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Booking {id} cancelled successfully");
#endif
        }

        // ========== DRIVER TRACKING ==========
        /// <summary>
        /// Gets the current driver location for a ride using the passenger-safe endpoint.
        /// Returns null if no location data is available or tracking hasn't started.
        /// </summary>
        /// <param name="rideId">The ride ID to get driver location for.</param>
        public async Task<DriverLocation?> GetDriverLocationAsync(string rideId)
        {
            try
            {
                // Use passenger-safe endpoint with email-based authorization
                var response = await _http.GetAsync($"/passenger/rides/{Uri.EscapeDataString(rideId)}/location");

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Ride doesn't exist
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdminApi] Ride not found: {rideId}");
#endif
                    return null;
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    // Not authorized to view this ride (email doesn't match booking)
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdminApi] Forbidden: Not authorized to view ride {rideId}");
#endif
                    return null;
                }

                response.EnsureSuccessStatusCode();

                // Try to deserialize as PassengerLocationResponse first
                var passengerResponse = await response.Content.ReadFromJsonAsync<PassengerLocationResponse>(_json);

                if (passengerResponse == null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdminApi] Failed to deserialize response for ride {rideId}");
#endif
                    return null;
                }

                // Check if tracking has started
                if (!passengerResponse.TrackingActive)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AdminApi] Tracking not started: {passengerResponse.Message}");
#endif
                    return null;
                }

                // Convert to DriverLocation
                var location = passengerResponse.ToDriverLocation();

#if DEBUG
                if (location != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AdminApi] Driver location: {location.Latitude:F6}, {location.Longitude:F6}, Age={location.AgeSeconds}s");
                }
#endif

                return location;
            }
            catch (HttpRequestException ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdminApi] HTTP error fetching driver location: {ex.Message}");
#endif
                return null;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AdminApi] Error fetching driver location: {ex.Message}");
#endif
                return null;
            }
        }
    }
}
