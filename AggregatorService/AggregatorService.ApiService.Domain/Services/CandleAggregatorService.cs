using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Services;

public class CandleAggregatorService
{
    private readonly Dictionary<string, CandleBuilder> _builders = new();
    private readonly TimeSpan[] _timeFrames;

    public CandleAggregatorService(IEnumerable<TimeSpan> timeFrames)
    {
        _timeFrames = timeFrames.ToArray();
    }

    public IEnumerable<Candle> Aggregate(Tick tick)
    {
        var closedCandles = new List<Candle>();

        foreach (var period in _timeFrames)
        {
            var bucketTime = GetBucketTime(tick.Timestamp, period);
            var key = $"{tick.Symbol.Value}_{period.TotalMinutes}";

            if (!_builders.TryGetValue(key, out var builder))
            {
                builder = new CandleBuilder(tick.Symbol, bucketTime, period);
                _builders[key] = builder;
            }

            if (builder.OpenTime != bucketTime)
            {
                closedCandles.Add(builder.Build());

                builder = new CandleBuilder(tick.Symbol, bucketTime, period);
                _builders[key] = builder;
            }

            builder.AddTick(tick);
        }

        return closedCandles;
    }

    private DateTimeOffset GetBucketTime(DateTimeOffset timestamp, TimeSpan period)
    {
        if (period.TotalMinutes >= 60)
        {
            return new DateTimeOffset(
                timestamp.Year, timestamp.Month, timestamp.Day,
                timestamp.Hour, 0, 0, TimeSpan.Zero);
        }

        var minute = (timestamp.Minute / (int)period.TotalMinutes) * (int)period.TotalMinutes;
        return new DateTimeOffset(
            timestamp.Year, timestamp.Month, timestamp.Day,
            timestamp.Hour, minute, 0, TimeSpan.Zero);
    }
}