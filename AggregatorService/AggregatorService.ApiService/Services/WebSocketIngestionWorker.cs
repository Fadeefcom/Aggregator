using AggregatorService.ApiService.Data;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AggregatorService.ApiService.Services;

public class WebSocketIngestionWorker : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly ILogger<WebSocketIngestionWorker> _logger;
    private readonly Uri _wsUri = new("ws://loadgenerator/ws/stream");

    public WebSocketIngestionWorker(
        IngestionChannel channel,
        ILogger<WebSocketIngestionWorker> logger)
    {
        _channel = channel;
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
                _logger.LogInformation("Connected to WebSocket stream");

                while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var tick = JsonSerializer.Deserialize<Tick>(json);

                    if (tick != null)
                    {
                        await _channel.WriteAsync(tick, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WebSocket connection lost. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}