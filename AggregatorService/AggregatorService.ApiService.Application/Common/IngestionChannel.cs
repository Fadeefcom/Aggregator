using AggregatorService.ApiService.Domain.Models;
using System.Threading.Channels;

namespace AggregatorService.ApiService.Application.Common;

public class IngestionChannel
{
    private readonly Channel<Tick> _channel;

    public IngestionChannel()
    {
        var options = new BoundedChannelOptions(10_000)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Tick>(options);
    }

    public ValueTask WriteAsync(Tick tick, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(tick, ct);
    }

    public ChannelReader<Tick> Reader => _channel.Reader;
}