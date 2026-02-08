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
    private readonly IConfiguration _configuration;

    private readonly ConcurrentDictionary<string, CandleBuilder> _activeCandles = new();
    private readonly ConcurrentDictionary<string, SourceStatus> _sourceStatuses = new();
    private readonly ConcurrentDictionary<string, Tick> _lastTicks = new();

    private readonly List<IAlertRule> _alertRules = new();

    private readonly HashSet<string> _allowedSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BTCUSD", "ETHUSD", "SOLUSD"
    };

    private readonly TimeSpan[] _timeFrames = new[]
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromHours(1)
    };

    public TickProcessor(IMemoryCache memoryCache, AlertChannel alertChannel, IConfiguration configuration)
    {
        _dedupCache = memoryCache;
        _alertChannel = alertChannel;
        _configuration = configuration;
        InitializeRules();
    }

    private void InitializeRules()
    {
        var rulesSection = _configuration.GetSection("Alerting:Rules");
        foreach (var ruleConfig in rulesSection.GetChildren())
        {
            var type = ruleConfig["Type"];
            if (string.Equals(type, "Price", StringComparison.OrdinalIgnoreCase))
            {
                var symbol = ruleConfig["Symbol"] ?? string.Empty;
                var min = ruleConfig.GetValue<decimal>("MinPrice");
                var max = ruleConfig.GetValue<decimal>("MaxPrice");
                _alertRules.Add(new PriceThresholdRule(symbol, min, max));
            }
            else if (string.Equals(type, "Volume", StringComparison.OrdinalIgnoreCase))
            {
                var multiplier = ruleConfig.GetValue<decimal>("Multiplier");
                _alertRules.Add(new VolumeSpikeRule(multiplier));
            }
        }
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

    public IEnumerable<Candle> UpdateMetricsAndAggregate(Tick tick)
    {
        _lastTicks.TryGetValue(tick.Symbol, out var previousTick);

        foreach (var rule in _alertRules)
        {
            if (rule.Evaluate(tick, previousTick, out var reason))
            {
                _alertChannel.Publish(new Alert(tick.Symbol, reason, tick.Timestamp, "Warning"));
            }
        }

        _lastTicks[tick.Symbol] = tick;

        UpdateSourceStatus(tick);
        return ProcessCandles(tick);
    }

    public SourceStatus GetSourceStatus(string source)
    {
        return _sourceStatuses.GetOrAdd(source, s => new SourceStatus
        {
            SourceName = s,
            IsOnline = true
        });
    }

    public IEnumerable<SourceStatus> GetAllSourceStatuses()
    {
        return _sourceStatuses.Values;
    }

    private void UpdateSourceStatus(Tick tick)
    {
        _sourceStatuses.AddOrUpdate(
            tick.Source,
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

    private IEnumerable<Candle> ProcessCandles(Tick tick)
    {
        var closedCandles = new List<Candle>();

        foreach (var period in _timeFrames)
        {
            var bucketTime = new DateTimeOffset(
                tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                tick.Timestamp.Hour, tick.Timestamp.Minute, 0, TimeSpan.Zero);

            if (period.TotalMinutes >= 60)
            {
                bucketTime = new DateTimeOffset(
                    tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                    tick.Timestamp.Hour, 0, 0, TimeSpan.Zero);
            }
            else if (period.TotalMinutes == 5)
            {
                var minute = (tick.Timestamp.Minute / 5) * 5;
                bucketTime = new DateTimeOffset(
                    tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                    tick.Timestamp.Hour, minute, 0, TimeSpan.Zero);
            }

            var key = $"{tick.Symbol}_{period.TotalMinutes}";

            var builder = _activeCandles.GetOrAdd(key, _ => new CandleBuilder
            {
                Symbol = tick.Symbol,
                OpenTime = bucketTime,
                Period = period
            });

            if (bucketTime > builder.OpenTime)
            {
                closedCandles.Add(builder.ToCandle());

                var newBuilder = new CandleBuilder
                {
                    Symbol = tick.Symbol,
                    OpenTime = bucketTime,
                    Period = period
                };

                newBuilder.AddTick(tick);
                _activeCandles[key] = newBuilder;
            }
            else
            {
                builder.AddTick(tick);
            }
        }

        return closedCandles;
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

        private decimal _sumPrice;
        private decimal _sumPriceSq;
        private decimal _totalPV;
        private int _count;

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

            _sumPrice += tick.Price;
            _sumPriceSq += tick.Price * tick.Price;
            _totalPV += tick.Price * tick.Volume;
            _count++;
        }

        public Candle ToCandle()
        {
            decimal avgPrice = 0;

            if (Volume > 0)
                avgPrice = _totalPV / Volume;
            else if (_count > 0)
                avgPrice = _sumPrice / _count;

            double volatility = 0;

            if (_count > 0)
            {
                double mean = (double)(_sumPrice / _count);
                double meanSq = (double)(_sumPriceSq / _count);
                double variance = Math.Max(0, meanSq - (mean * mean));
                volatility = Math.Sqrt(variance);
            }

            return new Candle
            {
                Symbol = Symbol,
                OpenTime = OpenTime,
                CloseTime = OpenTime.Add(Period),
                Period = Period,
                Open = Open,
                High = High,
                Low = Low,
                Close = Close,
                TotalVolume = Volume,
                AveragePrice = avgPrice,
                Volatility = (decimal)volatility
            };
        }
    }
}
