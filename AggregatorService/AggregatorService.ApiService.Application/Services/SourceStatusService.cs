using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using System.Collections.Concurrent;

namespace AggregatorService.ApiService.Application.Services;

public class SourceStatusService : ISourceStatusService
{
    private readonly ConcurrentDictionary<string, SourceStatus> _statuses = new();

    public void UpdateHeartbeat(string sourceName)
    {
        _statuses.AddOrUpdate(
            sourceName,
            key => new SourceStatus(key) { },
            (key, current) =>
            {
                current.IncrementTickCount();
                return current;
            });
    }

    public IEnumerable<SourceStatus> GetAllStatuses()
    {
        return _statuses.Values;
    }
}
