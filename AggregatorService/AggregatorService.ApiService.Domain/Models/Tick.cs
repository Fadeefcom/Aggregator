using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Domain.Models;

public class Tick
{
    public Guid Id { get; private set; }
    public Symbol Symbol { get; private set; }
    public decimal Price { get; private set; }
    public decimal Volume { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string Source { get; private set; }

    // Private constructor for EF Core
    private Tick() { }

    public Tick(Symbol symbol, decimal price, decimal volume, DateTimeOffset timestamp, string source)
    {
        if (price < 0) throw new ArgumentException("Price cannot be negative");

        Id = Guid.NewGuid();
        Symbol = symbol;
        Price = price;
        Volume = volume;
        Timestamp = timestamp;
        Source = source;
    }
}