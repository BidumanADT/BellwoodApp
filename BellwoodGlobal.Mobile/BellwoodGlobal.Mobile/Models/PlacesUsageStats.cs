namespace BellwoodGlobal.Mobile.Models;

/// <summary>
/// Tracks daily usage of Google Places API to stay within quotas and prevent unexpected costs.
/// </summary>
public sealed class PlacesUsageStats
{
    /// <summary>
    /// Date of usage (UTC, yyyy-MM-dd format)
    /// </summary>
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    
    /// <summary>
    /// Number of autocomplete sessions started (charged per session, not per request)
    /// </summary>
    public int AutocompleteSessions { get; set; }
    
    /// <summary>
    /// Number of autocomplete requests made (for rate limiting)
    /// </summary>
    public int AutocompleteRequests { get; set; }
    
    /// <summary>
    /// Number of Place Details API calls (charged per call)
    /// </summary>
    public int PlaceDetailsCalls { get; set; }
    
    /// <summary>
    /// Number of errors encountered (network, API, quota)
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Timestamp when quota was last exceeded (null if not exceeded today)
    /// </summary>
    public DateTime? QuotaExceededAt { get; set; }
    
    /// <summary>
    /// Whether autocomplete is currently disabled due to quota
    /// </summary>
    public bool IsDisabled { get; set; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
