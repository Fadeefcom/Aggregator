using AggregatorService.ApiService.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace AggregatorService.ApiService.Domain;

public class TickProcessor : ITickProcessor
{
    private readonly ILogger<TickProcessor> _logger;
    private readonly IMemoryCache _dedupCache;

    private readonly HashSet<string> _allowedSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BTCUSD", "ETHUSD", "SOLUSD"
    };

    private readonly ConcurrentDictionary<string, InstrumentMetrics> _metrics = new();

    public TickProcessor(ILogger<TickProcessor> logger, IMemoryCache memoryCache)
    {
        _logger = logger;
        _dedupCache = memoryCache;
    }

    public bool ShouldProcess(Tick tick)
    {
        return _allowedSymbols.Contains(tick.Symbol);
    }

    public Tick Normalize(Tick tick)
    {
        tick.Symbol = tick.Symbol.ToUpperInvariant();
        tick.Timestamp = tick.Timestamp.ToUniversalTime();
        return tick;
    }

    public bool IsDuplicate(Tick tick)
    {
        var key = $"{tick.Source}_{tick.Symbol}_{tick.Timestamp.ToUnixTimeMilliseconds()}_{tick.Price}";

        if (_dedupCache.TryGetValue(key, out _))
        {
            return true;
        }

        _dedupCache.Set(key, true, TimeSpan.FromSeconds(10));
        return false;
    }

    public void AggregateMetrics(Tick tick)
    {
        _metrics.AddOrUpdate(tick.Symbol,
            new InstrumentMetrics { Count = 1, SumPrice = tick.Price, MinPrice = tick.Price, MaxPrice = tick.Price },
            (key, current) =>
            {
                current.Count++;
                current.SumPrice += tick.Price;
                current.MinPrice = Math.Min(current.MinPrice, tick.Price);
                current.MaxPrice = Math.Max(current.MaxPrice, tick.Price);
                return current;
            });

    }

    private class InstrumentMetrics
    {
        public int Count { get; set; }
        public decimal SumPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal AveragePrice => Count == 0 ? 0 : SumPrice / Count;
    }
}