using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
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
    private readonly IConfiguration _configuration;

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
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sources = _configuration.GetSection("AggregatorSettings:Sources").Get<WebSocketSource[]>()
            ?.Where(s => s.Type == "WS").ToArray();

        if (sources == null || sources.Length == 0)
        {
            var uri = _configuration["AggregatorSettings:WebSocketUri"] ?? "ws://loadgenerator/ws/stream";
            sources = new[] { new WebSocketSource { Name = "Default_WS", Url = uri } };
        }

        var tasks = sources.Select(source => RunSocketLoop(source, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunSocketLoop(WebSocketSource source, CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * 4];
        var wsUri = new Uri(source.Url);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                _logger.LogInformation("Connecting to WebSocket {Source} at {Uri}", source.Name, wsUri);
                await ws.ConnectAsync(wsUri, stoppingToken);

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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "WebSocket connection error for {Source}", source.Name);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private class WebSocketSource
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}