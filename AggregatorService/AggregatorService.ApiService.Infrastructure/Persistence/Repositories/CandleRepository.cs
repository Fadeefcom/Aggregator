using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Infrastructure.Persistence.Repositories;

public class CandleRepository : ICandleRepository
{
    private readonly TradingDbContext _context;

    public CandleRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task AddBatchAsync(IEnumerable<Candle> candles, CancellationToken ct = default)
    {
        await _context.Candles.AddRangeAsync(candles, ct);
    }

    public async Task<Candle?> GetLastCandleAsync(Symbol symbol, TimeSpan period, CancellationToken ct = default)
    {
        return await _context.Candles
            .Where(c => c.Symbol == symbol && c.Period == period)
            .OrderByDescending(c => c.OpenTime)
            .FirstOrDefaultAsync(ct);
    }
}
