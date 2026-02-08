using AggregatorService.ApiService.Domain.Models;

namespace AggregatorService.ApiService.Application.Interfaces;

public interface INotificationChannel
{
    Task SendAsync(Alert alert, CancellationToken ct);
}