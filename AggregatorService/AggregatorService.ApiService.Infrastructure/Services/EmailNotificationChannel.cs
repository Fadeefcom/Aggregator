using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AggregatorService.ApiService.Infrastructure.Services;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(ILogger<EmailNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Alert alert, CancellationToken ct)
    {
        _logger.LogInformation("[EMAIL STUB] To: admin@trading.com, Subject: Alert {Symbol}, Body: {Message}", alert.Symbol, alert.Message);
        return Task.CompletedTask;
    }
}
