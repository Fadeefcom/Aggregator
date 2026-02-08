using AggregatorService.ApiService.Application.DTOs;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AggregatorService.ApiService.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEnumerable<INotificationChannel> channels, ILogger<NotificationService> logger)
    {
        _channels = channels;
        _logger = logger;
    }

    public async Task SendAlertAsync(AlertDto alert, CancellationToken ct = default)
    {
        var entity = new Alert(Symbol.Create(alert.Symbol), alert.Message, alert.Timestamp, alert.Severity);

        foreach (var channel in _channels)
        {
            try
            {
                await channel.SendAsync(entity, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert via {Channel}", channel.GetType().Name);
            }
        }
    }
}