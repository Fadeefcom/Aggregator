using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AggregatorService.ApiService.Services;

public class WebSocketIngestionWorker : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly ITickProcessor _processor;
    private readonly ILogger<WebSocketIngestionWorker> _logger;
    private readonly Uri _wsUri = new("ws://loadgenerator/ws/stream");

    public WebSocketIngestionWorker(
        IngestionChannel channel,
        ITickProcessor processor,
        ILogger<WebSocketIngestionWorker> logger)
    {
        _channel = channel;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * 4];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(_wsUri, stoppingToken);

                while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var rawTick = JsonSerializer.Deserialize<Tick>(json);

                    if (rawTick != null)
                    {
                        await _channel.WriteAsync(rawTick, stoppingToken);
                    }
                }
            }
            catch (Exception)
            {
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}