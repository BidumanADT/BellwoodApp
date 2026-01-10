using System.Text.Json;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Secure configuration service for sensitive application settings.
/// Reads from appsettings.json and environment variables.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, string> _settings = new();
    private bool _isInitialized = false;
    
    /// <summary>
    /// Initializes configuration by loading settings files asynchronously ON A BACKGROUND THREAD.
    /// This prevents blocking the UI thread during app startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[ConfigurationService] Already initialized, skipping");
#endif
            return;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[ConfigurationService] Starting async initialization...");
        var sw = System.Diagnostics.Stopwatch.StartNew();
#endif

        // PERFORMANCE FIX: Run file I/O on background thread to avoid blocking UI
        await Task.Run(async () =>
        {
            // Try to load appsettings.json (production/template)
            await TryLoadSettingsFileAsync("appsettings.json");
            
            // Try to load appsettings.Development.json (overrides production)
            // This file should be in .gitignore with actual keys
            await TryLoadSettingsFileAsync("appsettings.Development.json");
        });
        
        _isInitialized = true;

#if DEBUG
        sw.Stop();
        System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Initialization complete in {sw.ElapsedMilliseconds}ms. Loaded {_settings.Count} settings.");
#endif
    }
    
    /// <summary>
    /// Gets the Google Places API key.
    /// </summary>
    /// <returns>API key string</returns>
    /// <exception cref="InvalidOperationException">If key not found or service not initialized</exception>
    public string GetPlacesApiKey()
    {
        EnsureInitialized();
        return GetSetting("GooglePlacesApiKey", "Google Places API key");
    }
    
    /// <summary>
    /// Gets the Admin API base URL.
    /// </summary>
    public string GetAdminApiUrl()
    {
        EnsureInitialized();
        return GetSetting("AdminApiUrl", "Admin API URL");
    }
    
    /// <summary>
    /// Gets the Auth Server base URL.
    /// </summary>
    public string GetAuthServerUrl()
    {
        EnsureInitialized();
        return GetSetting("AuthServerUrl", "Auth Server URL");
    }
    
    /// <summary>
    /// Gets the Rides API base URL.
    /// </summary>
    public string GetRidesApiUrl()
    {
        EnsureInitialized();
        return GetSetting("RidesApiUrl", "Rides API URL");
    }
    
    // ========== PRIVATE HELPERS ==========
    
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "ConfigurationService has not been initialized. " +
                "Call InitializeAsync() during app startup before using any Get* methods.");
        }
    }
    
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
    
    private async Task TryLoadSettingsFileAsync(string filename)
    {
        try
        {
            // In MAUI, configuration files are embedded resources
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                
                if (loaded != null)
                {
                    // Use lock for thread-safety since we're on background thread
                    lock (_settings)
                    {
                        foreach (var kvp in loaded)
                        {
                            _settings[kvp.Key] = kvp.Value; // Overwrite if exists
                        }
                    }
                    
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ConfigurationService] Loaded {loaded.Count} settings from {filename}");
#endif
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
