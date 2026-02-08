using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain; // Added
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace AggregatorService.ApiService.Services;

public class TickProcessingService : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly ITickProcessor _processor; // Added dependency
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Counter<long> _processedCounter;
    private const int BatchSize = 1000;
    private readonly TimeSpan _statusSaveInterval = TimeSpan.FromSeconds(5);

    public TickProcessingService(
        IngestionChannel channel,
        ITickProcessor processor,
        IServiceScopeFactory scopeFactory,
        IMeterFactory meterFactory)
    {
        _channel = channel;
        _processor = processor;
        _scopeFactory = scopeFactory;
        var meter = meterFactory.Create("TradingSystem.Aggregator");
        _processedCounter = meter.CreateCounter<long>("ticks_processed");
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
                // 1. Centralized Processing Here (Moved from Workers)
                // This ensures backpressure handles CPU load too
                if (!_processor.ShouldProcess(tick)) continue;
                tick = _processor.Normalize(tick);
                if (_processor.IsDuplicate(tick)) continue;

                // 2. Add to Raw Buffer
                tickBuffer.Add(tick);

                // 3. Update Aggregates & Check for Closed Candles
                var closedCandle = _processor.UpdateMetricsAndAggregate(tick);
                if (closedCandle != null)
                {
                    candleBuffer.Add(closedCandle);
                }

                // 4. Batch Save
                if (tickBuffer.Count >= BatchSize)
                {
                    await SaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                }
            }

            // Flush remaining or update status periodically
            if (tickBuffer.Count > 0 || candleBuffer.Count > 0 || DateTime.UtcNow - lastStatusSave > _statusSaveInterval)
            {
                await SaveDataAsync(tickBuffer, candleBuffer, stoppingToken);
                await SaveStatusesAsync(stoppingToken); // New: Save Status
                lastStatusSave = DateTime.UtcNow;
            }
        }
    }

    private async Task SaveDataAsync(List<Tick> ticks, List<Candle> candles, CancellationToken ct)
    {
        if (ticks.Count == 0 && candles.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        if (ticks.Count > 0)
        {
            await db.Ticks.AddRangeAsync(ticks, ct);
            _processedCounter.Add(ticks.Count);
            ticks.Clear();
        }

        if (candles.Count > 0)
        {
            await db.Candles.AddRangeAsync(candles, ct);
            candles.Clear();
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SaveStatusesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        // Upsert logic would be ideal here, simplified for EF Core
        var sources = new[] { "REST_BINANCE", "WS_KRAKEN", "Exchange_0", "Exchange_1", "Exchange_2" }; // Known sources

        foreach (var sourceName in sources)
        {
            var status = _processor.GetSourceStatus(sourceName);
            var dbStatus = await db.SourceStatuses.FindAsync(new object[] { sourceName }, ct);

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