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

        public async Task<AcceptQuoteResponse> AcceptQuoteAsync(string quoteId)
        {
            using var res = await _http.PostAsync($"/quotes/{quoteId}/accept", null);

            if (res.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await res.Content.ReadFromJsonAsync<QuoteErrorResponse>(_json);
                throw new InvalidOperationException(error?.Error ?? "Cannot accept quote");
            }

            if (res.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("You don't have permission to accept this quote");
            }

            res.EnsureSuccessStatusCode();

            var result = await res.Content.ReadFromJsonAsync<AcceptQuoteResponse>(_json);

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"[AdminApi] Quote {quoteId} accepted successfully. Booking created: {result?.BookingId}");
#endif

            return result ?? throw new InvalidOperationException("Failed to accept quote");
        }

        public async Task CancelQuoteAsync(string quoteId)
        {
            using var res = await _http.PostAsync($"/quotes/{quoteId}/cancel", null);

            if (res.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await res.Content.ReadFromJsonAsync<QuoteErrorResponse>(_json);
                throw new InvalidOperationException(error?.Error ?? "Cannot cancel quote");
            }

            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Quote {quoteId} cancelled successfully");
#endif
        }

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

        // ========== BOOKER PROFILE ==========
        // HttpRequestException (with StatusCode) is intentionally not caught here so that
        // ProfileService can distinguish 401/403 from network/server errors for logging and retry.

        public async Task<BookerProfile?> GetProfileAsync()
        {
            var response = await _http.GetAsync("/profile");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BookerProfile>(_json);
        }

        public Task<BookerProfile?> GetBookerProfileAsync() => GetProfileAsync();

        public async Task UpdateProfileAsync(string firstName, string lastName, string? phoneNumber, string? emailAddress)
        {
            var body = new { firstName, lastName, phoneNumber, emailAddress };
            using var res = await _http.PutAsJsonAsync("/profile", body, _json);
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Profile updated: {firstName} {lastName}");
#endif
        }

        // ========== SAVED PASSENGERS ==========

        public async Task<List<SavedPassengerDto>> GetSavedPassengersAsync()
        {
            var items = await _http.GetFromJsonAsync<List<SavedPassengerDto>>("/profile/passengers", _json)
                        ?? new List<SavedPassengerDto>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] /profile/passengers -> {items.Count} items");
#endif
            return items;
        }

        public async Task<SavedPassengerDto?> CreateSavedPassengerAsync(SavePassengerRequest request)
        {
            using var res = await _http.PostAsJsonAsync("/profile/passengers", request, _json);
            res.EnsureSuccessStatusCode();

            var created = await res.Content.ReadFromJsonAsync<SavedPassengerDto>(_json);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Created passenger: {created?.FirstName} {created?.LastName} (id={created?.Id})");
#endif
            return created;
        }

        public async Task UpdateSavedPassengerAsync(Guid id, SavePassengerRequest request)
        {
            using var res = await _http.PutAsJsonAsync($"/profile/passengers/{id}", request, _json);
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Updated passenger id={id}");
#endif
        }

        public async Task DeleteSavedPassengerAsync(Guid id)
        {
            using var res = await _http.DeleteAsync($"/profile/passengers/{id}");

            // 404 is acceptable — record already gone
            if (res.StatusCode == HttpStatusCode.NotFound) return;
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Deleted passenger id={id}");
#endif
        }

        // ========== SAVED LOCATIONS ==========

        public async Task<List<SavedLocationDto>> GetSavedLocationsAsync()
        {
            var items = await _http.GetFromJsonAsync<List<SavedLocationDto>>("/profile/locations", _json)
                        ?? new List<SavedLocationDto>();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] /profile/locations -> {items.Count} items");
#endif
            return items;
        }

        public async Task<SavedLocationDto?> CreateSavedLocationAsync(SaveLocationRequest request)
        {
            using var res = await _http.PostAsJsonAsync("/profile/locations", request, _json);
            res.EnsureSuccessStatusCode();

            var created = await res.Content.ReadFromJsonAsync<SavedLocationDto>(_json);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Created location: {created?.Label} (id={created?.Id})");
#endif
            return created;
        }

        public async Task UpdateSavedLocationAsync(Guid id, SaveLocationRequest request)
        {
            using var res = await _http.PutAsJsonAsync($"/profile/locations/{id}", request, _json);
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Updated location id={id}");
#endif
        }

        public async Task DeleteSavedLocationAsync(Guid id)
        {
            using var res = await _http.DeleteAsync($"/profile/locations/{id}");

            // 404 is acceptable — record already gone
            if (res.StatusCode == HttpStatusCode.NotFound) return;
            res.EnsureSuccessStatusCode();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AdminApi] Deleted location id={id}");
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
