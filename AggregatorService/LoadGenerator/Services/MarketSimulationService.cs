using System.Collections.Concurrent;

namespace LoadGenerator.Services;

public class MarketSimulationService
{
    private readonly ConcurrentDictionary<string, ExchangeProfile> _profiles = new();

    public MarketSimulationService()
    {
        _profiles.TryAdd("exchange-1", new ExchangeProfile
        {
            Name = "Exchange_1",
            SupportedSymbols = ["BTCUSD", "ETHUSD"],
            BasePriceOffset = 0
        });

        _profiles.TryAdd("exchange-2", new ExchangeProfile
        {
            Name = "Exchange_2",
            SupportedSymbols = ["BTCUSD", "SOLUSD", "ADAUSD"],
            BasePriceOffset = 50 // Чуть дороже
        });

        _profiles.TryAdd("exchange-3", new ExchangeProfile
        {
            Name = "Exchange_3",
            SupportedSymbols = ["ETHUSD", "DOGEUSD"],
            BasePriceOffset = -20
        });
    }

    public ExchangeProfile GetProfile(string exchangeId)
    {
        return _profiles.GetOrAdd(exchangeId, id => new ExchangeProfile
        {
            Name = id,
            SupportedSymbols = ["BTCUSD", "ETHUSD", "SOLUSD"],
            BasePriceOffset = Random.Shared.Next(-100, 100)
        });
    }
}

public class ExchangeProfile
{
    public string Name { get; set; } = string.Empty;
    public string[] SupportedSymbols { get; set; } = [];
    public decimal BasePriceOffset { get; set; }
}