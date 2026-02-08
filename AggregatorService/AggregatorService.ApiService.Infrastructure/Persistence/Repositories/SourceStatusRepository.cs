using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AggregatorService.ApiService.Infrastructure.Persistence.Repositories;

public class SourceStatusRepository : ISourceStatusRepository
{
    private readonly TradingDbContext _context;

    public SourceStatusRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task<SourceStatus?> GetByNameAsync(string sourceName, CancellationToken ct = default)
    {
        return await _context.SourceStatuses
            .FirstOrDefaultAsync(s => s.SourceName == sourceName, ct);
    }

    public async Task AddAsync(SourceStatus status, CancellationToken ct = default)
    {
        await _context.SourceStatuses.AddAsync(status, ct);
    }

    public async Task<List<SourceStatus>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.SourceStatuses
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
