using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AggregatorService.ApiService.Infrastructure.Services;

public class ConsoleNotificationChannel : INotificationChannel
{
    private readonly ILogger<ConsoleNotificationChannel> _logger;

    public ConsoleNotificationChannel(ILogger<ConsoleNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Alert alert, CancellationToken ct)
    {
        _logger.LogWarning("[ALERT CONSOLE] {Timestamp}: {Symbol} - {Message}", alert.Timestamp, alert.Symbol, alert.Message);
        return Task.CompletedTask;
    }
}