using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AggregatorService.ApiService.Infrastructure.Persistence.Repositories;

public class TickRepository : ITickRepository
{
    private readonly TradingDbContext _context;

    public TickRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task AddBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct = default)
    {
        await _context.Ticks.AddRangeAsync(ticks, ct);
    }
    public async Task<DateTimeOffset?> GetLatestTickTimestampAsync(CancellationToken ct = default)
    {
        return await _context.Ticks
            .OrderByDescending(t => t.Timestamp)
            .Select(t => (DateTimeOffset?)t.Timestamp)
            .FirstOrDefaultAsync(ct);
    }
}
