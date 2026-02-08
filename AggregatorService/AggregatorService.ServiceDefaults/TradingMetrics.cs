using System.Diagnostics.Metrics;

namespace AggregatorService.ServiceDefaults;

public class TradingMetrics
{
    public const string MeterName = "Trading.Aggregator";
    private readonly Meter _meter;

    public Counter<long> TicksReceived { get; }
    public Counter<long> TicksProcessed { get; }
    public ObservableGauge<int> ChannelDepth { get; }

    public TradingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        TicksReceived = _meter.CreateCounter<long>("trading.ticks_received");
        TicksProcessed = _meter.CreateCounter<long>("trading.ticks_processed");

        ChannelDepth = _meter.CreateObservableGauge("trading.channel_depth", () => ChannelSizeProvider.CurrentSize);
    }
}

public static class ChannelSizeProvider
{
    public static int CurrentSize { get; set; }
}