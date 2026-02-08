using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Alerts;
using AggregatorService.ApiService.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace AggregatorService.ApiService.Domain;

public class TickProcessor : ITickProcessor
{
    private readonly IMemoryCache _dedupCache;
    private readonly AlertChannel _alertChannel;

    // State for Aggregation (OHLCV Builders)
    // Key: Symbol -> TimeWindow -> CandleBuilder
    private readonly ConcurrentDictionary<string, CandleBuilder> _activeCandles = new();

    // State for Source Monitoring
    private readonly ConcurrentDictionary<string, SourceStatus> _sourceStatuses = new();

    private readonly List<IAlertRule> _alertRules = new();

    private readonly HashSet<string> _allowedSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BTCUSD", "ETHUSD", "SOLUSD"
    };

    public TickProcessor(IMemoryCache memoryCache)
    {
        _dedupCache = memoryCache;
        InitializeRules();
    }

    private void InitializeRules()
    {
        // Configuration: These could be loaded from DB/Config
        _alertRules.Add(new PriceThresholdRule("BTCUSD", minPrice: 55000, maxPrice: 65000));
        _alertRules.Add(new VolumeSpikeRule(multiplier: 3.0m)); // Alert if volume is 3x previous
    }

    public bool ShouldProcess(Tick tick) => _allowedSymbols.Contains(tick.Symbol);

    public Tick Normalize(Tick tick)
    {
        tick.Symbol = tick.Symbol.ToUpperInvariant();
        tick.Timestamp = tick.Timestamp.ToUniversalTime();
        return tick;
    }

    public bool IsDuplicate(Tick tick)
    {
        var key = $"{tick.Source}_{tick.Symbol}_{tick.Timestamp.ToUnixTimeMilliseconds()}_{tick.Price}";
        if (_dedupCache.TryGetValue(key, out _)) return true;
        _dedupCache.Set(key, true, TimeSpan.FromSeconds(10));
        return false;
    }

    // Returns a Candle if a time period just closed, otherwise null
    public Candle? UpdateMetricsAndAggregate(Tick tick)
    {
        UpdateSourceStatus(tick);
        return ProcessCandle(tick);
    }

    public SourceStatus GetSourceStatus(string source)
    {
        return _sourceStatuses.GetOrAdd(source, s => new SourceStatus { SourceName = s, IsOnline = true });
    }

    private void UpdateSourceStatus(Tick tick)
    {
        _sourceStatuses.AddOrUpdate(tick.Source,
            new SourceStatus
            {
                SourceName = tick.Source,
                IsOnline = true,
                LastUpdate = DateTimeOffset.UtcNow,
                TicksCount = 1
            },
            (key, current) =>
            {
                current.IsOnline = true;
                current.LastUpdate = DateTimeOffset.UtcNow;
                current.TicksCount++;
                return current;
            });
    }

    private Candle? ProcessCandle(Tick tick)
    {
        // Simple 1-minute aggregation logic
        // We define the "Bucket" by rounding down to the nearest minute
        var bucketTime = new DateTimeOffset(
            tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
            tick.Timestamp.Hour, tick.Timestamp.Minute, 0, TimeSpan.Zero);

        var key = $"{tick.Symbol}_1m";

        var builder = _activeCandles.GetOrAdd(key, _ => new CandleBuilder
        {
            Symbol = tick.Symbol,
            OpenTime = bucketTime,
            Period = TimeSpan.FromMinutes(1)
        });

        // If the tick belongs to a NEW minute, the previous candle is DONE.
        if (bucketTime > builder.OpenTime)
        {
            var closedCandle = builder.ToCandle();

            // Start new candle
            var newBuilder = new CandleBuilder
            {
                Symbol = tick.Symbol,
                OpenTime = bucketTime,
                Period = TimeSpan.FromMinutes(1)
            };
            newBuilder.AddTick(tick);
            _activeCandles[key] = newBuilder;

            return closedCandle; // Return the closed candle to be saved
        }

        builder.AddTick(tick);
        return null; // Candle not yet closed
    }

    private class CandleBuilder
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTimeOffset OpenTime { get; set; }
        public TimeSpan Period { get; set; }

        public decimal Open { get; set; }
        public decimal High { get; set; } = decimal.MinValue;
        public decimal Low { get; set; } = decimal.MaxValue;
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        private bool _isFirst = true;

        public void AddTick(Tick tick)
        {
            if (_isFirst)
            {
                Open = tick.Price;
                _isFirst = false;
            }
            High = Math.Max(High, tick.Price);
            Low = Math.Min(Low, tick.Price);
            Close = tick.Price;
            Volume += tick.Volume;
        }

        public Candle ToCandle() => new Candle
        {
            Symbol = Symbol,
            OpenTime = OpenTime,
            CloseTime = OpenTime.Add(Period),
            Period = Period,
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            TotalVolume = Volume
        };
    }
}