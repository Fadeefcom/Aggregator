namespace AggregatorService.ApiService.Domain.Interfaces;

public record AlertDto(string Symbol, string Message, DateTimeOffset Timestamp, string Severity);

public interface INotificationService
{
    Task SendAlertAsync(AlertDto alert, CancellationToken ct = default);
}
