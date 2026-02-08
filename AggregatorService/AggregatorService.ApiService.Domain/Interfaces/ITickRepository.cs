using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Interfaces;

public interface ITickRepository
{
    Task AddBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct);
    Task<DateTimeOffset?> GetLatestTickTimestampAsync(CancellationToken ct = default);
}