using AggregatorService.ApiService.Data;

namespace AggregatorService.ApiService.Infrastructure.Persistence;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

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