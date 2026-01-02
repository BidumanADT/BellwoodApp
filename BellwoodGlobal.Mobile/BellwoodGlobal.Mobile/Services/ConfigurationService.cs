using System.Text.Json;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Secure configuration service for sensitive application settings.
/// Reads from appsettings.json and environment variables.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, string> _settings;
    
    public ConfigurationService()
    {
        _settings = LoadSettings();
    }
    
    /// <summary>
    /// Gets the Google Places API key.
    /// </summary>
    /// <returns>API key string</returns>
    /// <exception cref="InvalidOperationException">If key not found</exception>
    public string GetPlacesApiKey()
    {
        return GetSetting("GooglePlacesApiKey", "Google Places API key");
    }
    
    /// <summary>
    /// Gets the Admin API base URL.
    /// </summary>
    public string GetAdminApiUrl()
    {
        return GetSetting("AdminApiUrl", "Admin API URL");
    }
    
    /// <summary>
    /// Gets the Auth Server base URL.
    /// </summary>
    public string GetAuthServerUrl()
    {
        return GetSetting("AuthServerUrl", "Auth Server URL");
    }
    
    /// <summary>
    /// Gets the Rides API base URL.
    /// </summary>
    public string GetRidesApiUrl()
    {
        return GetSetting("RidesApiUrl", "Rides API URL");
    }
    
    // ========== PRIVATE HELPERS ==========
    
    private string GetSetting(string key, string friendlyName)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            // Check if value is an environment variable reference
            if (value.StartsWith("ENV:", StringComparison.OrdinalIgnoreCase))
            {
                var envVarName = value.Substring(4); // Remove "ENV:" prefix
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                
                if (string.IsNullOrEmpty(envValue))
                {
                    throw new InvalidOperationException(
                        $"{friendlyName} not found. " +
                        $"Set environment variable '{envVarName}' or update appsettings.json");
                }
                
                return envValue;
            }
            
            return value;
        }
        
        throw new InvalidOperationException(
            $"{friendlyName} not found in configuration. " +
            $"Check appsettings.json or appsettings.Development.json");
    }
    
    private Dictionary<string, string> LoadSettings()
    {
        var settings = new Dictionary<string, string>();
        
        // Try to load appsettings.json (production/template)
        TryLoadSettingsFile("appsettings.json", settings);
        
        // Try to load appsettings.Development.json (overrides production)
        // This file should be in .gitignore with actual keys
        TryLoadSettingsFile("appsettings.Development.json", settings);
        
        return settings;
    }
    
    private void TryLoadSettingsFile(string filename, Dictionary<string, string> settings)
    {
        try
        {
            var filePath = Path.Combine(FileSystem.AppDataDirectory, filename);
            
            // If not in AppDataDirectory, try next to executable (for development)
            if (!File.Exists(filePath))
            {
                // In MAUI, configuration files are embedded resources
                // We'll read from the assembly
                using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                        {
                            settings[kvp.Key] = kvp.Value; // Overwrite if exists
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Could not load {filename}: {ex.Message}");
#endif
            // Not critical - may not exist in all environments
        }
    }
}
