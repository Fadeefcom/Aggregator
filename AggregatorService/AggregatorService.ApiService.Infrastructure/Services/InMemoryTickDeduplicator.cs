using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AggregatorService.ApiService.Infrastructure.Services;

public class InMemoryTickDeduplicator : ITickDeduplicator
{
    private readonly IMemoryCache _cache;

    public InMemoryTickDeduplicator(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsDuplicate(Tick tick)
    {
        var key = $"dedup:{tick.Source}:{tick.Symbol.Value}:{tick.Timestamp.ToUnixTimeMilliseconds()}:{tick.Price}";

        if (_cache.TryGetValue(key, out _))
        {
            return true;
        }

        _cache.Set(key, true, TimeSpan.FromSeconds(10));
        return false;
    }
}
