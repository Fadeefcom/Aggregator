using AggregatorService.ApiService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AggregatorService.ApiService.Domain.Models;

public class Candle
{
    public Guid Id { get; set; }
    public Symbol Symbol { get; private set; }
    public DateTimeOffset OpenTime { get; set; }
    public DateTimeOffset CloseTime { get; set; }

    public TimeSpan Period { get; set; }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Volatility { get; set; }

    private Candle() { }

    public Candle(
        Symbol symbol,
        DateTimeOffset openTime,
        DateTimeOffset closeTime,
        TimeSpan period,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal totalVolume,
        decimal averagePrice,
        decimal volatility)
    {
        Id = Guid.NewGuid();
        Symbol = symbol;
        OpenTime = openTime;
        CloseTime = closeTime;
        Period = period;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        TotalVolume = totalVolume;
        AveragePrice = averagePrice;
        Volatility = volatility;
    }
}
