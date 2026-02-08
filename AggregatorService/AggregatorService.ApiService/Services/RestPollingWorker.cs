using AggregatorService.ApiService.Data;

namespace AggregatorService.ApiService.Services;

public class RestPollingWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IngestionChannel _channel;
    private readonly ILogger<RestPollingWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    public RestPollingWorker(
        IHttpClientFactory httpClientFactory,
        IngestionChannel channel,
        ILogger<RestPollingWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExchangeClient");
                var tick = await client.GetFromJsonAsync<Tick>("api/exchange/ticker/BTCUSD", stoppingToken);

                if (tick != null)
                {
                    await _channel.WriteAsync(tick, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to poll REST endpoint");
            }
        }
    }
}