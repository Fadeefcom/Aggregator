using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Application.Interfaces;

public interface IAlertService
{
    Task CheckAlertsAsync(Tick tick, CancellationToken ct = default);
}
