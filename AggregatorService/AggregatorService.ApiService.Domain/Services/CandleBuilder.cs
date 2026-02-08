using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Domain.Services;

public class CandleBuilder
{
    public Symbol Symbol { get; }
    public DateTimeOffset OpenTime { get; }
    public TimeSpan Period { get; }
    public decimal Open => _open;
    public decimal Close => _close;

    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;
    private decimal _volume;

    private decimal _sumPrice;
    private decimal _sumPriceSq;
    private decimal _totalPV;
    private int _count;
    private bool _isFirst = true;

    public CandleBuilder(Symbol symbol, DateTimeOffset openTime, TimeSpan period)
    {
        Symbol = symbol;
        OpenTime = openTime;
        Period = period;
        _high = decimal.MinValue;
        _low = decimal.MaxValue;
    }

    public void AddTick(Tick tick)
    {
        if (_isFirst)
        {
            _open = tick.Price;
            _isFirst = false;
        }

        _high = Math.Max(_high, tick.Price);
        _low = Math.Min(_low, tick.Price);
        _close = tick.Price;
        _volume += tick.Volume;

        _sumPrice += tick.Price;
        _sumPriceSq += tick.Price * tick.Price;
        _totalPV += tick.Price * tick.Volume;
        _count++;
    }

    public bool IsFinished(DateTimeOffset tickTime)
    {
        return tickTime >= OpenTime.Add(Period);
    }

    public Candle Build()
    {
        decimal avgPrice = 0;
        if (_volume > 0) avgPrice = _totalPV / _volume;
        else if (_count > 0) avgPrice = _sumPrice / _count;

        double volatility = 0;
        if (_count > 0)
        {
            double mean = (double)(_sumPrice / _count);
            double meanSq = (double)(_sumPriceSq / _count);
            double variance = Math.Max(0, meanSq - (mean * mean));
            volatility = Math.Sqrt(variance);
        }

        return new Candle(
            Symbol,
            OpenTime,
            OpenTime.Add(Period),
            Period,
            _open,
            _high,
            _low,
            _close,
            _volume,
            avgPrice,
            (decimal)volatility
        );
    }

    public Candle ToCandle()
    {
        var averagePrice = (Open + _high + _low + Close) / 4;
        var volatility = _high - _low;

        return new Candle(
            Symbol,
            OpenTime,
            OpenTime.Add(Period),
            Period,
            Open,
            _high,
            _low,
            Close,
            _volume,
            averagePrice,
            volatility
        );
    }
}