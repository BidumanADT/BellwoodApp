using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Tracks Google Places API usage to prevent quota overages and unexpected costs.
/// </summary>
public interface IPlacesUsageTracker
{
    /// <summary>
    /// Records the start of a new autocomplete session (charged per session).
    /// </summary>
    void RecordSessionStart();
    
    /// <summary>
    /// Records an autocomplete API request (for rate limiting, not billing).
    /// </summary>
    void RecordAutocompleteRequest();
    
    /// <summary>
    /// Records a Place Details API call (charged per call).
    /// </summary>
    void RecordPlaceDetailsCall();
    
    /// <summary>
    /// Records an API error.
    /// </summary>
    void RecordError();
    
    /// <summary>
    /// Checks if autocomplete is currently disabled due to quota limits.
    /// </summary>
    bool IsAutocompleteDisabled();
    
    /// <summary>
    /// Checks if we should show a warning (e.g., 80% of daily limit).
    /// </summary>
    bool ShouldShowWarning();
    
    /// <summary>
    /// Checks if rate limit is exceeded (too many requests in short time).
    /// </summary>
    bool IsRateLimited();
    
    /// <summary>
    /// Gets today's usage statistics.
    /// </summary>
    PlacesUsageStats GetTodayStats();
    
    /// <summary>
    /// Gets usage statistics for a specific date.
    /// </summary>
    PlacesUsageStats? GetStatsForDate(DateTime date);
    
    /// <summary>
    /// Exports usage data for the last N days (for monthly review).
    /// </summary>
    string ExportUsageReport(int days = 30);
    
    /// <summary>
    /// Manually resets quota (for testing or emergency override).
    /// </summary>
    void ResetQuota();
}
