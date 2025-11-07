using System;

namespace BellwoodGlobal.Core.Helpers
{
    public static class LocationHelper
    {
        /// <summary>
        /// Detects if a location string indicates an airport (contains "airport" or "fbo").
        /// </summary>
        public static bool IsAirportText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.ToLowerInvariant();
            return t.Contains("airport") || t.Contains("fbo");
        }

        /// <summary>
        /// Resolves a location from a picker or "New Location" input fields.
        /// </summary>
        public static string ResolveLocation(string? pickerSelection, string? newLabel, string? newAddress)
        {
            if (pickerSelection == "New Location")
                return $"{(newLabel ?? "").Trim()} - {(newAddress ?? "").Trim()}".Trim(' ', '-');
            return pickerSelection ?? "";
        }
    }
}