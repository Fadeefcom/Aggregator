using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain;
using AggregatorService.ServiceDefaults;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AggregatorService.ApiService.Services;

public class WebSocketIngestionWorker : BackgroundService
{
    private readonly IngestionChannel _channel;
    private readonly ITickProcessor _processor;
    private readonly TradingMetrics _metrics;
    private readonly ILogger<WebSocketIngestionWorker> _logger;
    private readonly Uri _wsUri;

    public WebSocketIngestionWorker(
        IngestionChannel channel,
        ITickProcessor processor,
        TradingMetrics metrics,
        ILogger<WebSocketIngestionWorker> logger,
        IConfiguration configuration)
    {
        _channel = channel;
        _processor = processor;
        _metrics = metrics;
        _logger = logger;
        _wsUri = new Uri(configuration["AggregatorSettings:WebSocketUri"] ?? "ws://loadgenerator/ws/stream");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * 4];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                _logger.LogInformation("Connecting to WebSocket at {Uri}", _wsUri);
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
                        _metrics.TicksReceived.Add(1, new KeyValuePair<string, object?>("source", rawTick.Source));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connection error");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}