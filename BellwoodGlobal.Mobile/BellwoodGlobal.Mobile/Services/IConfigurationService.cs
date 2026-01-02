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

    /// <summary>
    /// Gets the Admin API base URL.
    /// </summary>
    string GetAdminApiUrl();

    /// <summary>
    /// Gets the Auth Server base URL.
    /// </summary>
    string GetAuthServerUrl();

    /// <summary>
    /// Gets the Rides API base URL.
    /// </summary>
    string GetRidesApiUrl();
}
