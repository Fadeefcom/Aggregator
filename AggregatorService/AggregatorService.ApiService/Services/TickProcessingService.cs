using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain;
using AggregatorService.ServiceDefaults;
using System.Diagnostics;

namespace AggregatorService.ApiService.Services;

public class TickProcessingService : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly ITickProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TradingMetrics _metrics;
    private readonly int _batchSize;
    private readonly TimeSpan _statusSaveInterval;
    private readonly ILogger<TickProcessingService> _logger;

    public TickProcessingService(
        IngestionChannel channel,
        ITickProcessor processor,
        IServiceScopeFactory scopeFactory,
        TradingMetrics metrics,
        IConfiguration configuration,
        ILogger<TickProcessingService> logger)
    {
        _channel = channel;
        _processor = processor;
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _batchSize = configuration.GetValue<int>("AggregatorSettings:BatchSize", 1000);
        _statusSaveInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("AggregatorSettings:StatusSaveIntervalSeconds", 5));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickBuffer = new List<Tick>(_batchSize);
        var candleBuffer = new List<Candle>();
        var lastStatusSave = DateTime.UtcNow;

        try
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_channel.Reader.TryRead(out var tick))
                {
                    var latency = (DateTimeOffset.UtcNow - tick.Timestamp).TotalMilliseconds;
                    _metrics.ProcessingLatency.Record(
                        latency,
                        new KeyValuePair<string, object?>("source", tick.Source));

                    if (!_processor.ShouldProcess(tick)) continue;

                    tick = _processor.Normalize(tick);

                    if (_processor.IsDuplicate(tick)) continue;

                    tickBuffer.Add(tick);

                    var closedCandles = _processor.UpdateMetricsAndAggregate(tick);
                    foreach (var candle in closedCandles)
                    {
                        candleBuffer.Add(candle);
                        _metrics.CandlesGenerated.Add(
                            1,
                            new KeyValuePair<string, object?>("symbol", candle.Symbol));
                    }

                    _metrics.TicksProcessed.Add(
                        1,
                        new KeyValuePair<string, object?>("source", tick.Source));

                    if (tickBuffer.Count >= _batchSize)
                    {
                        await TrySaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                    }
                }

                if (tickBuffer.Count > 0 ||
                    candleBuffer.Count > 0 ||
                    DateTime.UtcNow - lastStatusSave > _statusSaveInterval)
                {
                    await TrySaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                    await TrySaveStatusesAsync(stoppingToken);
                    lastStatusSave = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "TickProcessingService loop failed");
        }
        finally
        {
            _logger.LogInformation("Graceful shutdown: flushing {TickCount} ticks and {CandleCount} candles...", tickBuffer.Count, candleBuffer.Count);

            using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await TrySaveDataAsync(tickBuffer, candleBuffer, shutdownCts.Token);
                await TrySaveStatusesAsync(shutdownCts.Token);
                _logger.LogInformation("Flush completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush data during shutdown");
            }
        }
    }

    private async Task TrySaveDataAsync(
        List<Tick> ticks,
        List<Candle> candles,
        CancellationToken ct)
    {
        try
        {
            await SaveDataAsync(ticks, candles, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving data batch to DB");
        }
    }

    private async Task TrySaveStatusesAsync(CancellationToken ct)
    {
        try
        {
            await SaveStatusesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving source statuses to DB");
        }
    }

    private async Task SaveDataAsync(
        List<Tick> ticks,
        List<Candle> candles,
        CancellationToken ct)
    {
        if (ticks.Count == 0 && candles.Count == 0) return;

        var sw = Stopwatch.StartNew();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        if (ticks.Count > 0)
        {
            await db.Ticks.AddRangeAsync(ticks, ct);
            ticks.Clear();
        }

        if (candles.Count > 0)
        {
            await db.Candles.AddRangeAsync(candles, ct);
            candles.Clear();
        }

        await db.SaveChangesAsync(ct);

        sw.Stop();
        _metrics.DbWriteDuration.Record(sw.Elapsed.TotalMilliseconds);
    }

    private async Task SaveStatusesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        var sources = _processor.GetAllSourceStatuses().Select(s => s.SourceName).ToList();

        if (sources.Count == 0)
        {
            sources = new List<string> {
            "REST_BINANCE",
            "WS_KRAKEN",
            "Exchange_0",
            "Exchange_1",
            "Exchange_2"
            };
        }

        foreach (var sourceName in sources)
        {
            var status = _processor.GetSourceStatus(sourceName);
            var dbStatus = await db.SourceStatuses.FindAsync(
                new object[] { sourceName },
                ct);

            if (dbStatus == null)
            {
                await db.SourceStatuses.AddAsync(status, ct);
            }
            else
            {
                dbStatus.IsOnline = status.IsOnline;
                dbStatus.LastUpdate = status.LastUpdate;
                dbStatus.TicksCount = status.TicksCount;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}