using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Application.Interfaces;

public interface ISourceStatusService
{
    void UpdateHeartbeat(string sourceName);
    IEnumerable<SourceStatus> GetAllStatuses();
}
