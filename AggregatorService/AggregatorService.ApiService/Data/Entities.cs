using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AggregatorService.ApiService.Data;

// 1. Raw Data (Existing)
public class Tick
{
    public Guid Id { get; set; }
    [MaxLength(20)] public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    [MaxLength(50)] public string Source { get; set; } = string.Empty;
}

// 2. Aggregated Data (New: OHLCV)
public class Candle
{
    public Guid Id { get; set; }
    [MaxLength(20)] public string Symbol { get; set; } = string.Empty;
    public DateTimeOffset OpenTime { get; set; }
    public DateTimeOffset CloseTime { get; set; }

    // Aggregation Window (1min, 5min, 1h)
    public TimeSpan Period { get; set; }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal TotalVolume { get; set; }
}

// 3. Instrument Metadata (New)
public class Instrument
{
    [Key, MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty; // e.g., BTC
    public string QuoteCurrency { get; set; } = string.Empty; // e.g., USD
    public string Type { get; set; } = "Crypto"; // Crypto, Forex, Stock
    public bool IsActive { get; set; } = true;
}

// 4. Source Status (New)
public class SourceStatus
{
    [Key, MaxLength(50)]
    public string SourceName { get; set; } = string.Empty; // e.g., "Binance_REST"
    public bool IsOnline { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public long TicksCount { get; set; }
    public string? LastError { get; set; }
}