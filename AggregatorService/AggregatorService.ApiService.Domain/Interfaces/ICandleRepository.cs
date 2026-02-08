using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Domain.Interfaces;

public interface ICandleRepository
{
    Task AddBatchAsync(IEnumerable<Candle> candles, CancellationToken ct);
    Task<Candle?> GetLastCandleAsync(Symbol symbol, TimeSpan period, CancellationToken ct = default);
}
