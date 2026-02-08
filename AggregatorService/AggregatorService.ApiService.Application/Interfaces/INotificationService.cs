namespace AggregatorService.ApiService.Domain.Interfaces;

public interface INotificationService
{
    Task SendAlertAsync(AlertDto alert, CancellationToken ct = default);
}
