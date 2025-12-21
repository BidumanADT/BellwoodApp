using System;

namespace BellwoodGlobal.Mobile.Helpers;

/// <summary>
/// Helper methods for handling DateTime display in the passenger app.
/// Addresses timezone conversion issues between backend and frontend.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Formats a DateTime for display, handling potential timezone mismatches.
    /// The backend stores times as local (Central Time) but System.Text.Json may serialize
    /// them as UTC, causing double-conversion when displayed.
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <param name="format">Format string (default: "g" for general date/time)</param>
    /// <returns>Formatted date/time string</returns>
    public static string FormatForDisplay(DateTime dateTime, string format = "g")
    {
        // If the DateTime is already marked as Local or Unspecified, use it directly
        // This prevents double-conversion
        if (dateTime.Kind == DateTimeKind.Local || dateTime.Kind == DateTimeKind.Unspecified)
        {
            return dateTime.ToString(format);
        }

        // If marked as UTC but came from backend (which stores local times),
        // treat it as local time
        // In a future backend fix, this should be resolved at the source
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToString(format);
    }

    /// <summary>
    /// Formats a DateTime for display with a friendly relative time if recent.
    /// </summary>
    /// <param name="dateTime">The DateTime to format</param>
    /// <returns>Formatted string like "Today at 2:30 PM" or "Tomorrow at 10:15 AM"</returns>
    public static string FormatFriendly(DateTime dateTime)
    {
        var displayTime = dateTime.Kind == DateTimeKind.Utc
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local)
            : dateTime;

        var now = DateTime.Now;
        var diff = displayTime.Date - now.Date;

        return diff.Days switch
        {
            0 => $"Today at {displayTime:t}",
            1 => $"Tomorrow at {displayTime:t}",
            -1 => $"Yesterday at {displayTime:t}",
            _ when diff.Days > 1 && diff.Days <= 7 => $"{displayTime:dddd} at {displayTime:t}",
            _ => displayTime.ToString("g")
        };
    }
}
