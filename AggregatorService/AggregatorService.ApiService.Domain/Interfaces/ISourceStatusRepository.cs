using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Interfaces;

public interface ISourceStatusRepository
{
    Task<SourceStatus?> GetByNameAsync(string sourceName, CancellationToken ct = default);
    Task AddAsync(SourceStatus status, CancellationToken ct = default);
    Task<List<SourceStatus>> GetAllAsync(CancellationToken ct = default);
}