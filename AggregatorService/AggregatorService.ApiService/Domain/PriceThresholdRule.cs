using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.Rules;

namespace AggregatorService.ApiService.Domain;

public class PriceThresholdRule : IAlertRule
{
    private readonly string _symbol;
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceThresholdRule(string symbol, decimal minPrice, decimal maxPrice)
    {
        _symbol = symbol;
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public bool Evaluate(Tick tick, Tick? previousTick, out string reason)
    {
        reason = string.Empty;
        if (!tick.Symbol.ToString().Equals(_symbol, StringComparison.OrdinalIgnoreCase)) return false;

        if (tick.Price > _maxPrice)
        {
            reason = $"Price {tick.Price} exceeded max threshold {_maxPrice}";
            return true;
        }
        if (tick.Price < _minPrice)
        {
            reason = $"Price {tick.Price} dropped below min threshold {_minPrice}";
            return true;
        }
        return false;
    }
}