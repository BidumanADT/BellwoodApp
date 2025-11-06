using System;
using System.Collections.Generic;
using System.Linq;

namespace BellwoodGlobal.Core.Helpers
{
    public static class CapacityValidator
    {
        // Vehicle capacity limits: (Passengers, Checked Bags, Carry-On Bags)
        private static readonly Dictionary<string, (int pax, int checkedBags, int carryOns)> VehicleCaps = new()
        {
            { "Sedan",    (2, 2, 4) },
            { "S-Class",  (2, 2, 4) },
            { "SUV",      (4, 4, 8) },
            { "Sprinter", (8, 10, 20) },
        };

        /// <summary>
        /// Evaluates if the given passenger/baggage counts fit within the vehicle class limits.
        /// </summary>
        /// <returns>Tuple: (within limits?, note, suggested vehicle class)</returns>
        public static (bool within, string? note, string? suggestion) Evaluate(
            int pax, int checkedBags, int carryOns, string vehicleClass)
        {
            if (!VehicleCaps.TryGetValue(vehicleClass, out var caps))
            {
                // Unknown class ? assume OK
                return (true, null, null);
            }

            var overPax = pax > caps.pax;
            var overCheck = checkedBags > caps.checkedBags;
            var overCarry = carryOns > caps.carryOns;

            if (!overPax && !overCheck && !overCarry)
                return (true, null, null);

            // Build a short note for JSON/email
            var reasons = new List<string>();
            if (overPax) reasons.Add($"pax {pax}/{caps.pax}");
            if (overCheck) reasons.Add($"checked {checkedBags}/{caps.checkedBags}");
            if (overCarry) reasons.Add($"carry-on {carryOns}/{caps.carryOns}");

            var suggestion = SuggestVehicle(pax, checkedBags, carryOns, vehicleClass);
            var note = $"Over capacity for {vehicleClass}: {string.Join(", ", reasons)}." +
                       (suggestion is not null ? $" Suggest {suggestion}." : "");

            return (false, note, suggestion);
        }

        /// <summary>
        /// Suggests the smallest vehicle class that fits the requirements.
        /// </summary>
        public static string? SuggestVehicle(int pax, int checkedBags, int carryOns, string current)
        {
            var order = new[] { "Sedan", "S-Class", "SUV", "Sprinter" };
            var start = Math.Max(0, Array.IndexOf(order, current) + 1);

            for (int i = start; i < order.Length; i++)
            {
                var cls = order[i];
                if (!VehicleCaps.TryGetValue(cls, out var caps)) continue;

                if (pax <= caps.pax && checkedBags <= caps.checkedBags && carryOns <= caps.carryOns)
                    return cls;
            }
            return null; // nothing fits (rare)
        }
    }
}