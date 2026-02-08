using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AggregatorService.Application.Services;

public class TickIngestionService : ITickIngestionService
{
    private readonly ITickRepository _tickRepository;
    private readonly ICandleRepository _candleRepository;
    private readonly ITickDeduplicator _deduplicator;
    private readonly CandleAggregatorService _aggregatorService;
    private readonly IAlertService _alertService;
    private readonly ISourceStatusService _sourceStatusService;
    private readonly IMetricsService _metrics;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TickIngestionService> _logger;

    private readonly HashSet<string> _allowedSymbols;

    public TickIngestionService(
        ITickRepository tickRepository,
        ICandleRepository candleRepository,
        ITickDeduplicator deduplicator,
        CandleAggregatorService aggregatorService,
        IAlertService alertService,
        ISourceStatusService sourceStatusService,
        IMetricsService metrics,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<TickIngestionService> logger)
    {
        _tickRepository = tickRepository;
        _candleRepository = candleRepository;
        _deduplicator = deduplicator;
        _aggregatorService = aggregatorService;
        _alertService = alertService;
        _sourceStatusService = sourceStatusService;
        _metrics = metrics;
        _unitOfWork = unitOfWork;
        _logger = logger;

        var symbols = configuration.GetSection("AggregatorSettings:AllowedSymbols").Get<string[]>()
                      ?? ["BTCUSD", "ETHUSD", "SOLUSD"];
        _allowedSymbols = new HashSet<string>(symbols, StringComparer.OrdinalIgnoreCase);
    }

    public async Task ProcessBatchAsync(IEnumerable<Tick> ticks, CancellationToken ct = default)
    {
        var validTicks = new List<Tick>();
        var newCandles = new List<Candle>();

        var stopwatch = Stopwatch.StartNew();

        foreach (var tick in ticks)
        {
            if (!_allowedSymbols.Contains(tick.Symbol.Value))
                continue;

            var latency = (DateTimeOffset.UtcNow - tick.Timestamp).TotalMilliseconds;
            _metrics.RecordLatency(tick.Source, latency);

            if (_deduplicator.IsDuplicate(tick))
                continue;

            _sourceStatusService.UpdateHeartbeat(tick.Source);

            await _alertService.CheckAlertsAsync(tick, ct);

            var candles = _aggregatorService.Aggregate(tick);
            foreach (var candle in candles)
            {
                newCandles.Add(candle);
                _metrics.RecordCandleGenerated(candle.Symbol.Value);
            }

            validTicks.Add(tick);
            _metrics.RecordTickProcessed(tick.Source);
        }

        if (validTicks.Count == 0 && newCandles.Count == 0) return;

        try
        {
            var dbSw = Stopwatch.StartNew();

            if (validTicks.Count > 0)
                await _tickRepository.AddBatchAsync(validTicks, ct);

            if (newCandles.Count > 0)
                await _candleRepository.AddBatchAsync(newCandles, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            dbSw.Stop();
            _metrics.RecordDbWriteDuration(dbSw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch. Ticks: {TickCount}, Candles: {CandleCount}", validTicks.Count, newCandles.Count);
            throw;
        }

        stopwatch.Stop();
    }
}