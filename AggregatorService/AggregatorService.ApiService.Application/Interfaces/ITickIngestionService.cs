using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Application.Interfaces;

public interface ITickIngestionService
{
    Task ProcessBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct = default);
}