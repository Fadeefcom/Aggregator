using AggregatorService.ApiService.Data;

namespace AggregatorService.ApiService.Domain;

public interface ITickProcessor
{
    bool ShouldProcess(Tick tick);
    Tick Normalize(Tick tick);
    bool IsDuplicate(Tick tick);

    // Changed: Returns a closed candle if ready
    Candle? UpdateMetricsAndAggregate(Tick tick);
    SourceStatus GetSourceStatus(string source);
}