using AggregatorService.ApiService.Data;

namespace AggregatorService.ApiService.Domain;

public interface ITickProcessor
{
    bool ShouldProcess(Tick tick);
    Tick Normalize(Tick tick);
    bool IsDuplicate(Tick tick);
    Candle? UpdateMetricsAndAggregate(Tick tick);

    // Monitoring Methods
    SourceStatus GetSourceStatus(string source);
    IEnumerable<SourceStatus> GetAllSourceStatuses(); // New method
}