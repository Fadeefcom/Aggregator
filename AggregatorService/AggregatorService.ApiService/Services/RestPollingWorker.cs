using AggregatorService.ApiService.Data;
using AggregatorService.ServiceDefaults;

namespace AggregatorService.ApiService.Services;

public class RestPollingWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IngestionChannel _channel;
    private readonly TradingMetrics _metrics;
    private readonly ILogger<RestPollingWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);
    private readonly string[] _symbols;

    public RestPollingWorker(
        IHttpClientFactory httpClientFactory,
        IngestionChannel channel,
        TradingMetrics metrics,
        ILogger<RestPollingWorker> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _channel = channel;
        _metrics = metrics;
        _logger = logger;
        _symbols = configuration.GetSection("AggregatorSettings:AllowedSymbols").Get<string[]>()
                   ?? new[] { "BTCUSD" };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExchangeClient");

                foreach (var symbol in _symbols)
                {
                    var tick = await client.GetFromJsonAsync<Tick>($"api/exchange/ticker/{symbol}", stoppingToken);

                    if (tick != null)
                    {
                        await _channel.WriteAsync(tick, stoppingToken);
                        _metrics.TicksReceived.Add(1, new KeyValuePair<string, object?>("source", tick.Source));
                        _logger.LogDebug("RestPollingWorker received tick for {Symbol}", tick.Symbol);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to poll REST endpoint");
            }
        }
    }
}