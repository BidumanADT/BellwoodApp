namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Secure configuration service for sensitive application settings.
/// Uses platform-specific secure storage in production.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    // Storage key for Places API key in SecureStorage
    private const string PlacesApiKeyStorageKey = "GooglePlacesApiKey";
    
    /// <summary>
    /// Gets the Google Places API key.
    /// </summary>
    /// <returns>API key string</returns>
    /// <exception cref="InvalidOperationException">If key not found in production</exception>
    public string GetPlacesApiKey()
    {
#if DEBUG
        // DEBUG MODE: Use hardcoded key for development
        // TODO: Replace with build secrets in CI/CD pipeline
        // This key should be injected at build time, not hardcoded
        return "AIzaSyCDu1jdljMdXvcl9tG7O6cJBw8f2h0sUIY";
#else
        // PRODUCTION MODE: Retrieve from secure storage
        // Key should be stored during app first-run or deployment
        try
        {
            var key = SecureStorage.GetAsync(PlacesApiKeyStorageKey).Result;
            
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException(
                    "Places API key not found in secure storage. " +
                    "Ensure the key is configured during app deployment.");
            }
            
            return key;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve Places API key from secure storage: {ex.Message}", 
                ex);
        }
#endif
    }
    
    /// <summary>
    /// Stores the Google Places API key in secure storage.
    /// Used during app deployment or first-run setup.
    /// </summary>
    /// <param name="apiKey">The API key to store</param>
    public async Task SetPlacesApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        
        await SecureStorage.SetAsync(PlacesApiKeyStorageKey, apiKey);
    }
}
