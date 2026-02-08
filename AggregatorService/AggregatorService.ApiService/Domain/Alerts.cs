namespace AggregatorService.ApiService.Domain.Alerts;

using AggregatorService.ApiService.Data;

public record Alert(string Symbol, string Message, DateTimeOffset Timestamp, string Severity);

// Strategy Interface for Rules
public interface IAlertRule
{
    bool Evaluate(Tick tick, Tick? previousTick, out string failureReason);
}

// Strategy Interface for Notifications
public interface INotificationChannel
{
    Task SendAsync(Alert alert, CancellationToken ct);
}

// Rule Implementation: Price Threshold
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
        if (!tick.Symbol.Equals(_symbol, StringComparison.OrdinalIgnoreCase)) return false;

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

// Rule Implementation: Sharp Volume Change (e.g., > 200% of previous tick)
public class VolumeSpikeRule : IAlertRule
{
    private readonly decimal _multiplier;

    public VolumeSpikeRule(decimal multiplier = 2.0m)
    {
        _multiplier = multiplier;
    }

    public bool Evaluate(Tick tick, Tick? previousTick, out string reason)
    {
        reason = string.Empty;
        if (previousTick == null || previousTick.Volume == 0) return false;

        if (tick.Volume > previousTick.Volume * _multiplier)
        {
            reason = $"Volume spike detected! Current: {tick.Volume}, Previous: {previousTick.Volume}";
            return true;
        }
        return false;
    }
}