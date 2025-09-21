using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public class QuoteService : IQuoteService
{
    public Task<QuoteEstimate> EstimateAsync(string pickup, string dropoff, DateTime when, CancellationToken ct = default)
    {
        // Placeholder logic: simple fake fare calc
        var milesGuess = Math.Max(7, (pickup?.Length ?? 0 + dropoff?.Length ?? 0) % 22);
        var baseFare = 35m;
        var perMile = 3.5m;
        var est = new QuoteEstimate
        {
            VehicleClass = "Executive Sedan",
            EstimatedFare = baseFare + perMile * milesGuess,
            EstimatedEta = TimeSpan.FromMinutes(15)
        };
        return Task.FromResult(est);
    }
}
