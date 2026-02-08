using AggregatorService.ApiService.Domain.Models;
using System.Threading.Channels;

namespace AggregatorService.ApiService.Application.Common;

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
