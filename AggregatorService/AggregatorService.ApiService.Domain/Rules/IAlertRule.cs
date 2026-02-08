using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Rules;

public interface IAlertRule
{
    bool Evaluate(Tick currentTick, Tick? previousTick, out string reason);
}
