using AggregatorService.ApiService.Data;
using System.Diagnostics.Metrics;

namespace AggregatorService.ApiService.Services;

public class TickProcessingService : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Counter<long> _processedCounter;
    private const int BatchSize = 1000;

    public TickProcessingService(
        IngestionChannel channel,
        IServiceScopeFactory scopeFactory,
        IMeterFactory meterFactory)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        var meter = meterFactory.Create("TradingSystem.Aggregator");
        _processedCounter = meter.CreateCounter<long>("ticks_processed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<Tick>(BatchSize);

        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_channel.Reader.TryRead(out var tick))
            {
                buffer.Add(tick);

                if (buffer.Count >= BatchSize)
                {
                    await SaveBatchAsync(buffer, stoppingToken);
                }
            }

            if (buffer.Count > 0)
            {
                await SaveBatchAsync(buffer, stoppingToken);
            }
        }
    }

    private async Task SaveBatchAsync(List<Tick> buffer, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();

        await dbContext.Ticks.AddRangeAsync(buffer, ct);
        await dbContext.SaveChangesAsync(ct);

        _processedCounter.Add(buffer.Count);
        buffer.Clear();
    }
}
