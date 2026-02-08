using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Services;

public class CandleAggregator
{
    private readonly Dictionary<string, CandleBuilder> _builders = new();
    private readonly TimeSpan[] _timeFrames;

    public CandleAggregator(IEnumerable<TimeSpan> timeFrames)
    {
        _timeFrames = timeFrames.ToArray();
    }

    public IEnumerable<Candle> Aggregate(Tick tick)
    {
        var closedCandles = new List<Candle>();
        foreach (var period in _timeFrames)
        {
            var key = $"{tick.Symbol.Value}_{period.TotalMinutes}";
            if (!_builders.TryGetValue(key, out var builder))
            {
                builder = new CandleBuilder(tick.Symbol, period, tick.Timestamp);
                _builders[key] = builder;
            }

            // Logic to check if candle is closed
            if (builder.IsFinished(tick.Timestamp))
            {
                closedCandles.Add(builder.Build());
                _builders[key] = new CandleBuilder(tick.Symbol, period, tick.Timestamp);
                _builders[key].AddTick(tick);
            }
            else
            {
                builder.AddTick(tick);
            }
        }
        return closedCandles;
    }
}
