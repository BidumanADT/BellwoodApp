using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BellwoodGlobal.Mobile.Models
{
    public static class VehicleRules
    {
        public sealed record Capacity(int Pax, int Checked, int CarryOn);

        // Consult with Bellwood for their exact standards as needed
        private static readonly Dictionary<string, Capacity> Limits = new()
        {
            { "Sedan", new Capacity(Pax: 2, Checked: 2, CarryOn: 4) },
            { "S-Class", new Capacity(Pax: 2, Checked: 2, CarryOn: 4) },
            { "SUV", new Capacity(Pax: 4, Checked: 4, CarryOn: 8) },
            { "Sprinter", new Capacity(Pax: 8, Checked: 10, CarryOn: 20) },
        };

        public static bool Fits(string vehicleClass, int pax, int checkedBags, int carryOnBags)
        {
            if (!Limits.TryGetValue(vehicleClass, out var cap)) return true; // unknown = permissive
            return pax <= cap.Pax && checkedBags <= cap.Checked && carryOnBags <= cap.CarryOn;
        }
        
        // Find the smallest class that fits; returns null if current fits or no class fits.
        public static string? SuggestUpgrade(string vehicleClass, int pax, int checkedBags, int carryOnBags)
        {
            if (Fits(vehicleClass, pax, checkedBags, carryOnBags)) return null;

            // Scan in increasing order of capacity
            foreach (var (cls, cap) in Limits)
            {
                if (pax <= cap.Pax && checkedBags <= cap.Checked && carryOnBags <= cap.CarryOn)
                {
                    if (cls != vehicleClass) return cls;
                }
            }
            return null;
        }
    }
}
