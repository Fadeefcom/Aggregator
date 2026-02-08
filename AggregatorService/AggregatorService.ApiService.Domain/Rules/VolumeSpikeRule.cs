using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Rules;

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
