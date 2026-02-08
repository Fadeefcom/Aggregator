using System.ComponentModel.DataAnnotations;

namespace AggregatorService.ApiService.Data;

public class Tick
{
    public Guid Id { get; set; }
    [MaxLength(20)] public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    [MaxLength(50)] public string Source { get; set; } = string.Empty;
}

public class Candle
{
    public Guid Id { get; set; }
    [MaxLength(20)] public string Symbol { get; set; } = string.Empty;
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
}

public class Instrument
{
    [Key, MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public string Type { get; set; } = "Crypto";
    public bool IsActive { get; set; } = true;
}

public class SourceStatus
{
    [Key, MaxLength(50)]
    public string SourceName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public long TicksCount { get; set; }
    public string? LastError { get; set; }
}