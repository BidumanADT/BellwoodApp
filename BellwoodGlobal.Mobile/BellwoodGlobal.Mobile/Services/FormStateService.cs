using System;
using System.Text.Json;
using System.Threading.Tasks;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services
{
    /// <summary>
    /// Implementation of form state persistence using MAUI Preferences.
    /// State is stored per-user using email as the key suffix.
    /// </summary>
    public class FormStateService : IFormStateService
    {
        private const string QuoteKeyPrefix = "QuotePage_FormState";
        private const string BookingKeyPrefix = "BookRidePage_FormState";
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false, // Compact storage
            PropertyNameCaseInsensitive = true
        };

        // ===== Helper: Get User-Specific Key =====
        
        private static string GetUserSpecificKey(string prefix)
        {
            try
            {
                // Get current user's email from SecureStorage (set during login)
                var userEmail = SecureStorage.GetAsync("user_email").Result;
                
                if (string.IsNullOrWhiteSpace(userEmail))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] WARNING: No user_email in SecureStorage, using global key");
#endif
                    return prefix; // Fallback to global key if no user logged in
                }
                
                // Create user-specific key: "QuotePage_FormState_alice.morgan@example.com"
                var userKey = $"{prefix}_{userEmail}";
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Using user-specific key: {userKey}");
#endif
                
                return userKey;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error getting user key: {ex.Message}, using global key");
#endif
                return prefix; // Fallback on error
            }
        }

        // ===== Quote Form =====

        public Task SaveQuoteFormStateAsync(QuotePageState state)
        {
            try
            {
                var key = GetUserSpecificKey(QuoteKeyPrefix);
                var json = JsonSerializer.Serialize(state, JsonOptions);
                Preferences.Set(key, json);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Saved Quote form state for current user ({json.Length} chars)");
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
                var key = GetUserSpecificKey(QuoteKeyPrefix);
                var json = Preferences.Get(key, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Quote form state found for current user");
#endif
                    return Task.FromResult<QuotePageState?>(null);
                }
                
                var state = JsonSerializer.Deserialize<QuotePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Quote form state for current user (last modified: {state?.LastModified})");
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
                var key = GetUserSpecificKey(QuoteKeyPrefix);
                Preferences.Remove(key);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[FormStateService] Cleared Quote form state for current user");
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
            var key = GetUserSpecificKey(QuoteKeyPrefix);
            var json = Preferences.Get(key, string.Empty);
            var hasSaved = !string.IsNullOrWhiteSpace(json);
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FormStateService] HasSavedQuoteForm for current user: {hasSaved}");
#endif
            
            return hasSaved;
        }

        // ===== Booking Form =====

        public Task SaveBookingFormStateAsync(BookRidePageState state)
        {
            try
            {
                var key = GetUserSpecificKey(BookingKeyPrefix);
                var json = JsonSerializer.Serialize(state, JsonOptions);
                Preferences.Set(key, json);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Saved Booking form state for current user ({json.Length} chars)");
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
                var key = GetUserSpecificKey(BookingKeyPrefix);
                var json = Preferences.Get(key, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Booking form state found for current user");
#endif
                    return Task.FromResult<BookRidePageState?>(null);
                }
                
                var state = JsonSerializer.Deserialize<BookRidePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Booking form state for current user (last modified: {state?.LastModified})");
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
                var key = GetUserSpecificKey(BookingKeyPrefix);
                Preferences.Remove(key);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[FormStateService] Cleared Booking form state for current user");
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
            var key = GetUserSpecificKey(BookingKeyPrefix);
            var json = Preferences.Get(key, string.Empty);
            var hasSaved = !string.IsNullOrWhiteSpace(json);
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FormStateService] HasSavedBookingForm for current user: {hasSaved}");
#endif
            
            return hasSaved;
        }
    }
}
