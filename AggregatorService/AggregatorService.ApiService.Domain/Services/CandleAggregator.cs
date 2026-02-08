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
                var bucketTime = new DateTimeOffset(
                    tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                    tick.Timestamp.Hour, tick.Timestamp.Minute, 0, TimeSpan.Zero);

                builder = new CandleBuilder(tick.Symbol, bucketTime, period);
                _builders[key] = builder;
            }
            
            if (builder.IsFinished(tick.Timestamp))
            {
                var bucketTime = new DateTimeOffset(
                    tick.Timestamp.Year, tick.Timestamp.Month, tick.Timestamp.Day,
                    tick.Timestamp.Hour, tick.Timestamp.Minute, 0, TimeSpan.Zero);

                closedCandles.Add(builder.Build());
                _builders[key] = new CandleBuilder(tick.Symbol, bucketTime, period);
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
