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
    private const int BatchSize = 1000;
    private readonly TimeSpan _statusSaveInterval = TimeSpan.FromSeconds(5);

    public TickProcessingService(
        IngestionChannel channel,
        ITickProcessor processor,
        IServiceScopeFactory scopeFactory,
        TradingMetrics metrics)
    {
        _channel = channel;
        _processor = processor;
        _scopeFactory = scopeFactory;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickBuffer = new List<Tick>(BatchSize);
        var candleBuffer = new List<Candle>();
        var lastStatusSave = DateTime.UtcNow;

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

                if (tickBuffer.Count >= BatchSize)
                {
                    await SaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                }
            }

            if (tickBuffer.Count > 0 ||
                candleBuffer.Count > 0 ||
                DateTime.UtcNow - lastStatusSave > _statusSaveInterval)
            {
                await SaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                await SaveStatusesAsync(stoppingToken);
                lastStatusSave = DateTime.UtcNow;
            }
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

        var sources = new[]
        {
            "REST_BINANCE",
            "WS_KRAKEN",
            "Exchange_0",
            "Exchange_1",
            "Exchange_2"
        };

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
