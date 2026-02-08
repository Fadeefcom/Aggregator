using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Domain.Interfaces;

public interface ITickDeduplicator
{
    bool IsDuplicate(Tick tick);
}