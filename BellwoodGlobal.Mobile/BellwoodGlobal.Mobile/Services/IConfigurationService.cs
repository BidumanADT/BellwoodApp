namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Provides secure access to application configuration values.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the Google Places API key.
    /// In DEBUG mode, returns hardcoded key for development.
    /// In RELEASE mode, retrieves from secure storage.
    /// </summary>
    string GetPlacesApiKey();
}
