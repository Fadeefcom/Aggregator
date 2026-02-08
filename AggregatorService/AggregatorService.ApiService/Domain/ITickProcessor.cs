using AggregatorService.ApiService.Data;

namespace AggregatorService.ApiService.Domain;

public interface ITickProcessor
{
    bool ShouldProcess(Tick tick);
    Tick Normalize(Tick tick);
    bool IsDuplicate(Tick tick);
    void AggregateMetrics(Tick tick);
}