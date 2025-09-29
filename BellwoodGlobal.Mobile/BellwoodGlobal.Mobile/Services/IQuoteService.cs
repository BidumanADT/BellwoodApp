using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

public interface IQuoteService
{
    Task<QuoteEstimate> EstimateAsync(string pickup, string dropoff, DateTime when, CancellationToken ct = default);
}
