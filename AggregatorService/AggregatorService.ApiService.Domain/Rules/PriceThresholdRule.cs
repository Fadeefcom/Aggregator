using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Domain.Rules;

public class PriceThresholdRule : IAlertRule
{
    private readonly Symbol _symbol;
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceThresholdRule(Symbol symbol, decimal minPrice, decimal maxPrice)
    {
        _symbol = symbol;
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public bool Evaluate(Tick tick, Tick? previousTick, out string reason)
    {
        reason = string.Empty;

        if (tick.Symbol != _symbol) return false;

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
