namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Provides secure access to application configuration values.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Initializes the configuration service asynchronously.
    /// Must be called during app startup before using any Get* methods.
    /// </summary>
    /// <returns>A task representing the async initialization</returns>
    Task InitializeAsync();

    /// <summary>
    /// Gets the Google Places API key for Android.
    /// In DEBUG mode, returns hardcoded key for development.
    /// In RELEASE mode, retrieves from secure storage.
    /// </summary>
    string GetPlacesApiKey();

    /// <summary>
    /// Gets the Google Places API key for iOS.
    /// In DEBUG mode, returns value from appsettings.Development.json.
    /// In RELEASE mode, retrieves from secure storage / environment.
    /// </summary>
    string GetPlacesApiKeyIos();

    /// <summary>
    /// Gets the Admin API base URL.
    /// </summary>
    string GetAdminApiUrl();

    /// <summary>
    /// Gets the Auth Server base URL.
    /// </summary>
    string GetAuthServerUrl();

}
