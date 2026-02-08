namespace LoadGenerator;

public class Worker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IHttpClientFactory httpClientFactory, ILogger<Worker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(RunGeneratorAsync($"Exchange_{i}", stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunGeneratorAsync(string sourceName, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("aggregator");
        var random = new Random();

        while (!ct.IsCancellationRequested)
        {
            var tick = new
            {
                Symbol = "BTCUSD",
                Price = (decimal)random.Next(50000, 60000),
                Volume = (decimal)random.NextDouble(),
                Timestamp = DateTimeOffset.UtcNow,
                Source = sourceName
            };

            try
            {
                await client.PostAsJsonAsync("/api/ingestion", tick, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tick");
            }

            await Task.Delay(10, ct);
        }
    }
}