using System;
using System.Text.Json;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    /// <summary>
    /// Implementation of form state persistence using MAUI Preferences.
    /// </summary>
    public class FormStateService : IFormStateService
    {
        private const string QuoteKey = "QuotePage_FormState";
        private const string BookingKey = "BookRidePage_FormState";
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false, // Compact storage
            PropertyNameCaseInsensitive = true
        };

        // ===== Quote Form =====

        public Task SaveQuoteFormStateAsync(QuotePageState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, JsonOptions);
                Preferences.Set(QuoteKey, json);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Saved Quote form state ({json.Length} chars)");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error saving Quote state: {ex.Message}");
#endif
            }
            
            return Task.CompletedTask;
        }

        public Task<QuotePageState?> LoadQuoteFormStateAsync()
        {
            try
            {
                var json = Preferences.Get(QuoteKey, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Quote form state found");
#endif
                    return Task.FromResult<QuotePageState?>(null);
                }
                
                var state = JsonSerializer.Deserialize<QuotePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Quote form state (last modified: {state?.LastModified})");
#endif
                
                return Task.FromResult(state);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error loading Quote state: {ex.Message}");
#endif
                return Task.FromResult<QuotePageState?>(null);
            }
        }

        public Task ClearQuoteFormStateAsync()
        {
            try
            {
                Preferences.Remove(QuoteKey);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[FormStateService] Cleared Quote form state");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error clearing Quote state: {ex.Message}");
#endif
            }
            
            return Task.CompletedTask;
        }

        public bool HasSavedQuoteForm()
        {
            var json = Preferences.Get(QuoteKey, string.Empty);
            return !string.IsNullOrWhiteSpace(json);
        }

        // ===== Booking Form =====

        public Task SaveBookingFormStateAsync(BookRidePageState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, JsonOptions);
                Preferences.Set(BookingKey, json);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Saved Booking form state ({json.Length} chars)");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error saving Booking state: {ex.Message}");
#endif
            }
            
            return Task.CompletedTask;
        }

        public Task<BookRidePageState?> LoadBookingFormStateAsync()
        {
            try
            {
                var json = Preferences.Get(BookingKey, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Booking form state found");
#endif
                    return Task.FromResult<BookRidePageState?>(null);
                }
                
                var state = JsonSerializer.Deserialize<BookRidePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Booking form state (last modified: {state?.LastModified})");
#endif
                
                return Task.FromResult(state);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error loading Booking state: {ex.Message}");
#endif
                return Task.FromResult<BookRidePageState?>(null);
            }
        }

        public Task ClearBookingFormStateAsync()
        {
            try
            {
                Preferences.Remove(BookingKey);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[FormStateService] Cleared Booking form state");
#endif
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error clearing Booking state: {ex.Message}");
#endif
            }
            
            return Task.CompletedTask;
        }

        public bool HasSavedBookingForm()
        {
            var json = Preferences.Get(BookingKey, string.Empty);
            return !string.IsNullOrWhiteSpace(json);
        }
    }
}
