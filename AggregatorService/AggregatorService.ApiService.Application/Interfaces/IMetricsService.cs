namespace AggregatorService.ApiService.Application.Interfaces;

public interface IMetricsService
{
    void RecordTickProcessed(string source);
    void RecordLatency(string source, double latencyMs);
    void RecordCandleGenerated(string symbol);
    void RecordDbWriteDuration(double durationMs);
}
