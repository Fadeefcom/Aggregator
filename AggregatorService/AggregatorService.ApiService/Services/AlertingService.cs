using System.Threading.Channels;
using AggregatorService.ApiService.Domain.Alerts;

namespace AggregatorService.ApiService.Services;

// The buffer class
public class AlertChannel
{
    private readonly Channel<Alert> _channel;

    public AlertChannel()
    {
        _channel = Channel.CreateUnbounded<Alert>(); // Alerts are critical, we don't drop them usually
    }

    public void Publish(Alert alert) => _channel.Writer.TryWrite(alert);
    public ChannelReader<Alert> Reader => _channel.Reader;
}

// The background worker
public class AlertNotificationWorker : BackgroundService
{
    private readonly AlertChannel _channel;
    private readonly IEnumerable<INotificationChannel> _notificationChannels;
    private readonly ILogger<AlertNotificationWorker> _logger;

    public AlertNotificationWorker(
        AlertChannel channel,
        IEnumerable<INotificationChannel> notificationChannels,
        ILogger<AlertNotificationWorker> logger)
    {
        _channel = channel;
        _notificationChannels = notificationChannels;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var alert in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Fan-out to all channels in parallel
                var tasks = _notificationChannels.Select(c => c.SendAsync(alert, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process alert for {Symbol}", alert.Symbol);
            }
        }
    }
}