using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;

namespace AggregatorService.ApiService.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly TradingDbContext _context;

    public EfUnitOfWork(TradingDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}