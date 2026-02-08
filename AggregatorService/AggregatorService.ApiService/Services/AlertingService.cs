using System.Threading.Channels;
using AggregatorService.ApiService.Domain.Alerts;

namespace AggregatorService.ApiService.Services;

public class AlertChannel
{
    private readonly Channel<Alert> _channel;

    public AlertChannel()
    {
        _channel = Channel.CreateUnbounded<Alert>();
    }

    public void Publish(Alert alert) => _channel.Writer.TryWrite(alert);
    public ChannelReader<Alert> Reader => _channel.Reader;
}


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
        try
        {
            await foreach (var alert in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var tasks = _notificationChannels.Select(async c =>
                    {
                        try
                        {
                            await c.SendAsync(alert, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send alert via {ChannelType}", c.GetType().Name);
                        }
                    });

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process alert for {Symbol}", alert.Symbol);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL: AlertNotificationWorker crashed and stopped processing alerts.");
        }
    }
}