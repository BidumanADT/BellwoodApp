using System.Text;
using System.Text.Json;
using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Tracks Google Places API usage with persistent storage and quota enforcement.
/// </summary>
public sealed class PlacesUsageTracker : IPlacesUsageTracker
{
    // Quota limits (conservative estimates based on free tier)
    // Free tier: 10,000 autocomplete sessions/month ? 322/day
    // We set conservative limits with safety margin
    private const int MaxSessionsPerDay = 300;         // ~9,300/month (well within 10k free tier)
    private const int MaxDetailsPerDay = 300;          // ~9,300/month (conservative estimate)
    private const int MaxRequestsPer10Seconds = 10;    // Rate limiting (burst protection)
    private const int MaxDetailsPer60Seconds = 5;      // Rate limiting (abuse protection)
    
    // Warning thresholds
    private const double WarningThresholdPercent = 0.8; // 80%
    private const double DisableThresholdPercent = 0.95; // 95%
    
    private const string StatsKeyPrefix = "PlacesUsage_";
    
    // Rate limiting tracking (in-memory, resets on app restart)
    private readonly Queue<DateTime> _recentAutocompleteRequests = new();
    private readonly Queue<DateTime> _recentDetailsRequests = new();
    
    public void RecordSessionStart()
    {
        var stats = LoadTodayStats();
        stats.AutocompleteSessions++;
        stats.LastUpdated = DateTime.UtcNow;
        SaveStats(stats);
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Session started. Total today: {stats.AutocompleteSessions}");
#endif
    }
    
    public void RecordAutocompleteRequest()
    {
        var stats = LoadTodayStats();
        stats.AutocompleteRequests++;
        stats.LastUpdated = DateTime.UtcNow;
        SaveStats(stats);
        
        // Track for rate limiting
        _recentAutocompleteRequests.Enqueue(DateTime.UtcNow);
        CleanupOldRequests(_recentAutocompleteRequests, TimeSpan.FromSeconds(10));
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Autocomplete request. Total today: {stats.AutocompleteRequests}");
#endif
    }
    
    public void RecordPlaceDetailsCall()
    {
        var stats = LoadTodayStats();
        stats.PlaceDetailsCalls++;
        stats.LastUpdated = DateTime.UtcNow;
        
        // Check if we've hit the hard limit
        if (stats.PlaceDetailsCalls >= MaxDetailsPerDay * DisableThresholdPercent)
        {
            stats.IsDisabled = true;
            stats.QuotaExceededAt = DateTime.UtcNow;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] ?? QUOTA EXCEEDED! Disabling autocomplete.");
#endif
        }
        
        SaveStats(stats);
        
        // Track for rate limiting
        _recentDetailsRequests.Enqueue(DateTime.UtcNow);
        CleanupOldRequests(_recentDetailsRequests, TimeSpan.FromMinutes(1));
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Place Details call. Total today: {stats.PlaceDetailsCalls}");
#endif
    }
    
    public void RecordError()
    {
        var stats = LoadTodayStats();
        stats.ErrorCount++;
        stats.LastUpdated = DateTime.UtcNow;
        SaveStats(stats);
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Error recorded. Total today: {stats.ErrorCount}");
#endif
    }
    
    public bool IsAutocompleteDisabled()
    {
        var stats = LoadTodayStats();
        
        // Check if manually disabled due to quota
        if (stats.IsDisabled)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[PlacesUsageTracker] Autocomplete is DISABLED (quota exceeded)");
#endif
            return true;
        }
        
        // Check if we've hit session or details limit
        var sessionLimit = stats.AutocompleteSessions >= MaxSessionsPerDay * DisableThresholdPercent;
        var detailsLimit = stats.PlaceDetailsCalls >= MaxDetailsPerDay * DisableThresholdPercent;
        
        if (sessionLimit || detailsLimit)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Autocomplete DISABLED: Sessions={stats.AutocompleteSessions}/{MaxSessionsPerDay}, Details={stats.PlaceDetailsCalls}/{MaxDetailsPerDay}");
#endif
            
            // Auto-disable
            stats.IsDisabled = true;
            stats.QuotaExceededAt = DateTime.UtcNow;
            SaveStats(stats);
            return true;
        }
        
        return false;
    }
    
    public bool ShouldShowWarning()
    {
        var stats = LoadTodayStats();
        
        var sessionWarning = stats.AutocompleteSessions >= MaxSessionsPerDay * WarningThresholdPercent;
        var detailsWarning = stats.PlaceDetailsCalls >= MaxDetailsPerDay * WarningThresholdPercent;
        
        return sessionWarning || detailsWarning;
    }
    
    public bool IsRateLimited()
    {
        CleanupOldRequests(_recentAutocompleteRequests, TimeSpan.FromSeconds(10));
        CleanupOldRequests(_recentDetailsRequests, TimeSpan.FromMinutes(1));
        
        var autocompleteBurst = _recentAutocompleteRequests.Count >= MaxRequestsPer10Seconds;
        var detailsBurst = _recentDetailsRequests.Count >= MaxDetailsPer60Seconds;
        
        if (autocompleteBurst || detailsBurst)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] RATE LIMITED: Autocomplete={_recentAutocompleteRequests.Count}/10s, Details={_recentDetailsRequests.Count}/60s");
#endif
        }
        
        return autocompleteBurst || detailsBurst;
    }
    
    public PlacesUsageStats GetTodayStats() => LoadTodayStats();
    
    public PlacesUsageStats? GetStatsForDate(DateTime date)
    {
        var key = GetStatsKey(date);
        var json = Preferences.Get(key, null);
        
        if (string.IsNullOrWhiteSpace(json))
            return null;
        
        try
        {
            return JsonSerializer.Deserialize<PlacesUsageStats>(json);
        }
        catch
        {
            return null;
        }
    }
    
    public string ExportUsageReport(int days = 30)
    {
        var report = new StringBuilder();
        report.AppendLine("Google Places API Usage Report");
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"Period: Last {days} days");
        report.AppendLine();
        report.AppendLine("Date       | Sessions | Requests | Details | Errors");
        report.AppendLine("-----------|----------|----------|---------|-------");
        
        var totalSessions = 0;
        var totalRequests = 0;
        var totalDetails = 0;
        var totalErrors = 0;
        
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.AddDays(-i).Date;
            var stats = GetStatsForDate(date);
            
            if (stats != null)
            {
                report.AppendLine($"{stats.Date} | {stats.AutocompleteSessions,8} | {stats.AutocompleteRequests,8} | {stats.PlaceDetailsCalls,7} | {stats.ErrorCount,6}");
                
                totalSessions += stats.AutocompleteSessions;
                totalRequests += stats.AutocompleteRequests;
                totalDetails += stats.PlaceDetailsCalls;
                totalErrors += stats.ErrorCount;
            }
        }
        
        report.AppendLine("-----------|----------|----------|---------|-------");
        report.AppendLine($"TOTAL      | {totalSessions,8} | {totalRequests,8} | {totalDetails,7} | {totalErrors,6}");
        report.AppendLine();
        report.AppendLine($"Average sessions/day: {totalSessions / days}");
        report.AppendLine($"Average details/day: {totalDetails / days}");
        report.AppendLine($"Error rate: {(totalErrors / (double)Math.Max(1, totalRequests) * 100):F2}%");
        report.AppendLine();
        report.AppendLine("Quota Limits (Daily):");
        report.AppendLine($"  Max sessions: {MaxSessionsPerDay}");
        report.AppendLine($"  Max details: {MaxDetailsPerDay}");
        report.AppendLine($"  Warning threshold: {WarningThresholdPercent * 100}%");
        report.AppendLine($"  Disable threshold: {DisableThresholdPercent * 100}%");
        
        return report.ToString();
    }
    
    public void ResetQuota()
    {
        var stats = LoadTodayStats();
        stats.IsDisabled = false;
        stats.QuotaExceededAt = null;
        SaveStats(stats);
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine("[PlacesUsageTracker] Quota manually reset");
#endif
    }
    
    // ===== PRIVATE HELPERS =====
    
    private PlacesUsageStats LoadTodayStats()
    {
        var today = DateTime.UtcNow.Date;
        var key = GetStatsKey(today);
        var json = Preferences.Get(key, null);
        
        if (string.IsNullOrWhiteSpace(json))
        {
            // New day, create fresh stats
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Creating fresh stats for {today:yyyy-MM-dd}");
#endif
            return new PlacesUsageStats { Date = today.ToString("yyyy-MM-dd") };
        }
        
        try
        {
            var stats = JsonSerializer.Deserialize<PlacesUsageStats>(json);
            
            // Sanity check: if stored date doesn't match today, reset
            if (stats?.Date != today.ToString("yyyy-MM-dd"))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Date mismatch (stored={stats?.Date}, today={today:yyyy-MM-dd}). Creating fresh stats.");
#endif
                return new PlacesUsageStats { Date = today.ToString("yyyy-MM-dd") };
            }
            
            return stats!;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Error loading stats: {ex.Message}. Creating fresh stats.");
#endif
            // Corrupted data, reset
            return new PlacesUsageStats { Date = today.ToString("yyyy-MM-dd") };
        }
    }
    
    private void SaveStats(PlacesUsageStats stats)
    {
        var key = GetStatsKey(DateTime.Parse(stats.Date));
        var json = JsonSerializer.Serialize(stats);
        Preferences.Set(key, json);
        
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[PlacesUsageTracker] Stats saved: {stats.Date} | Sessions={stats.AutocompleteSessions}, Requests={stats.AutocompleteRequests}, Details={stats.PlaceDetailsCalls}");
#endif
    }
    
    private static string GetStatsKey(DateTime date) => $"{StatsKeyPrefix}{date:yyyyMMdd}";
    
    private static void CleanupOldRequests(Queue<DateTime> queue, TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        while (queue.Count > 0 && queue.Peek() < cutoff)
        {
            queue.Dequeue();
        }
    }
}
