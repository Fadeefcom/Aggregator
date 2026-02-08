using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.Rules;
using AggregatorService.ApiService.Domain.Services;
using AggregatorService.ApiService.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace AggregatorService.ApiService.Application.Services;

public class TickProcessor : ITickProcessor
{
    private readonly IMemoryCache _dedupCache;
    private readonly AlertChannel _alertChannel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TickProcessor> _logger;

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

    public TickProcessor(
        IMemoryCache memoryCache,
        AlertChannel alertChannel,
        IConfiguration configuration,
        ILogger<TickProcessor> logger)
    {
        _dedupCache = memoryCache;
        _alertChannel = alertChannel;
        _configuration = configuration;
        _logger = logger;

        var configuredSymbols = configuration.GetSection("AggregatorSettings:AllowedSymbols").Get<string[]>();
        if (configuredSymbols != null && configuredSymbols.Length > 0)
        {
            _allowedSymbols = new HashSet<string>(configuredSymbols, StringComparer.OrdinalIgnoreCase);
        }

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
                _alertRules.Add(new PriceThresholdRule(Symbol.Create(symbol), min, max));
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
        if (tick.Timestamp.Offset != TimeSpan.Zero)
        {
            return new Tick(
                tick.Symbol,
                tick.Price,
                tick.Volume,
                tick.Timestamp.ToUniversalTime(),
                tick.Source
            );
        }

        return tick;
    }

    public bool IsDuplicate(Tick tick)
    {
        var key = $"{tick.Source}_{tick.Symbol.Value}_{tick.Timestamp.ToUnixTimeMilliseconds()}_{tick.Price}";
        if (_dedupCache.TryGetValue(key, out _)) return true;

        _dedupCache.Set(key, true, TimeSpan.FromSeconds(10));
        return false;
    }

    public IEnumerable<Candle> UpdateMetricsAndAggregate(Tick tick)
    {
        _lastTicks.TryGetValue(tick.Symbol.Value, out var previousTick);

        foreach (var rule in _alertRules)
        {
            if (rule.Evaluate(tick, previousTick, out var reason))
            {
                _alertChannel.Publish(new Alert(tick.Symbol, reason, tick.Timestamp, "Warning"));
            }
        }

        _lastTicks[tick.Symbol.Value] = tick;

        UpdateSourceStatus(tick);
        return ProcessCandles(tick);
    }

    public SourceStatus GetSourceStatus(string source)
    {
        return _sourceStatuses.GetOrAdd(source, s => new SourceStatus(s));
    }

    public IEnumerable<SourceStatus> GetAllSourceStatuses()
    {
        return _sourceStatuses.Values;
    }

    private void UpdateSourceStatus(Tick tick)
    {
        _sourceStatuses.AddOrUpdate(
            tick.Source,
            sourceName =>
            {
                var s = new SourceStatus(sourceName);
                s.IncrementTickCount();
                return s;
            },
            (key, current) =>
            {
                current.IncrementTickCount();
                return current;
            });
    }

    private IEnumerable<Candle> ProcessCandles(Tick tick)
    {
        var closedCandles = new List<Candle>();

        foreach (var period in _timeFrames)
        {
            DateTimeOffset bucketTime;

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
            else
            {
                bucketTime = new DateTimeOffset(
                    tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                    tick.Timestamp.Hour, tick.Timestamp.Minute, 0, TimeSpan.Zero);
            }

            var key = $"{tick.Symbol.Value}_{period.TotalMinutes}";

            var builder = _activeCandles.GetOrAdd(key, _ =>
                new CandleBuilder(tick.Symbol, bucketTime, period));

            if (bucketTime > builder.OpenTime)
            {
                var closedCandle = builder.ToCandle();
                closedCandles.Add(closedCandle);

                _logger.LogDebug("Candle closed: {Symbol} {Period} Open:{Open} Close:{Close}",
                    closedCandle.Symbol, closedCandle.Period, closedCandle.Open, closedCandle.Close);

                var newBuilder = new CandleBuilder(tick.Symbol, bucketTime, period);
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
}