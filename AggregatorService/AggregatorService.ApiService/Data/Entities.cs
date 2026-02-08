namespace AggregatorService.ApiService.Data;

public class Tick
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTimeOffset Timestamp { get; set; }   
    public string Source { get; set; } = string.Empty;
}
