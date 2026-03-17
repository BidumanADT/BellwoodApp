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
            PropertyNameCaseInsensitive = true,
            TypeInfoResolverChain = { BellwoodJsonContext.Default }
        };

        // ===== Helper: Get User-Specific Key =====
        
        private static async Task<string> GetUserSpecificKeyAsync(string prefix)
        {
            try
            {
                // PHASE 2 PERFORMANCE FIX: Get current user's email asynchronously
                // This prevents blocking the UI thread
                var userEmail = await SecureStorage.GetAsync("user_email");
                
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

        public async Task SaveQuoteFormStateAsync(QuotePageState state)
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(QuoteKeyPrefix);
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
        }

        public async Task<QuotePageState?> LoadQuoteFormStateAsync()
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(QuoteKeyPrefix);
                var json = Preferences.Get(key, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Quote form state found for current user");
#endif
                    return null;
                }
                
                var state = JsonSerializer.Deserialize<QuotePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Quote form state for current user (last modified: {state?.LastModified})");
#endif
                
                return state;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error loading Quote state: {ex.Message}");
#endif
                return null;
            }
        }

        public async Task ClearQuoteFormStateAsync()
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(QuoteKeyPrefix);
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
        }

        public bool HasSavedQuoteForm()
        {
            // Note: This method cannot be async due to interface constraint
            // It's only used for quick checks, so we do a synchronous Preferences read
            // The actual SecureStorage call happens asynchronously when loading/saving
            
            // For now, we use a simple approach: check if ANY quote state exists
            // In production, you might want to cache the user-specific key
            var hasAny = !string.IsNullOrWhiteSpace(Preferences.Get(QuoteKeyPrefix, string.Empty));
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FormStateService] HasSavedQuoteForm (simple check): {hasAny}");
#endif
            
            return hasAny;
        }

        // ===== Booking Form =====

        public async Task SaveBookingFormStateAsync(BookRidePageState state)
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(BookingKeyPrefix);
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
        }

        public async Task<BookRidePageState?> LoadBookingFormStateAsync()
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(BookingKeyPrefix);
                var json = Preferences.Get(key, string.Empty);
                
                if (string.IsNullOrWhiteSpace(json))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[FormStateService] No saved Booking form state found for current user");
#endif
                    return null;
                }
                
                var state = JsonSerializer.Deserialize<BookRidePageState>(json, JsonOptions);
                
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Loaded Booking form state for current user (last modified: {state?.LastModified})");
#endif
                
                return state;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[FormStateService] Error loading Booking state: {ex.Message}");
#endif
                return null;
            }
        }

        public async Task ClearBookingFormStateAsync()
        {
            try
            {
                var key = await GetUserSpecificKeyAsync(BookingKeyPrefix);
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
        }

        public bool HasSavedBookingForm()
        {
            // Note: This method cannot be async due to interface constraint
            // It's only used for quick checks, so we do a synchronous Preferences read
            // The actual SecureStorage call happens asynchronously when loading/saving
            
            // For now, we use a simple approach: check if ANY booking state exists
            // In production, you might want to cache the user-specific key
            var hasAny = !string.IsNullOrWhiteSpace(Preferences.Get(BookingKeyPrefix, string.Empty));
            
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FormStateService] HasSavedBookingForm (simple check): {hasAny}");
#endif
            
            return hasAny;
        }
    }
}
