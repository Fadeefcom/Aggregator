using System.Text.Json.Serialization;

namespace AggregatorService.ApiService.Application.DTOs;

public class TickDto
{
    public string Symbol { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Volume { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string Source { get; set; } = string.Empty;
}
