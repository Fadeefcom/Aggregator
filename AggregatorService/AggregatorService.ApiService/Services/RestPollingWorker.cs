using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.DTOs;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
using AggregatorService.ServiceDefaults;
using System.Net;
using System.Text.Json;

namespace AggregatorService.ApiService.Services;

public class RestPollingWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IngestionChannel _channel;
    private readonly TradingMetrics _metrics;
    private readonly ILogger<RestPollingWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string[] _globalSymbols;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        _configuration = configuration;
        _globalSymbols = configuration.GetSection("AggregatorSettings:AllowedSymbols").Get<string[]>()
                   ?? new[] { "BTCUSD" };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sources = _configuration.GetSection("AggregatorSettings:Sources").Get<ExchangeSource[]>()
            ?.Where(s => s.Type == "REST").ToArray();

        if (sources == null || sources.Length == 0)
        {
            sources = new[] { new ExchangeSource { Name = "Default_REST", IntervalSeconds = 1, Url = "api/exchange/ticker" } };
        }

        var tasks = sources.Select(source => RunPollingLoop(source, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunPollingLoop(ExchangeSource source, CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(0.1, source.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        var symbolsToPoll = (source.Symbols != null && source.Symbols.Length > 0)
            ? source.Symbols
            : _globalSymbols;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExchangeClient");

                foreach (var symbol in symbolsToPoll)
                {
                    var url = $"{source.Url}/{symbol}?source={source.Name}";
                    using var response = await client.GetAsync(url, stoppingToken);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var rawTick = await response.Content.ReadFromJsonAsync<TickDto>(_jsonOptions, stoppingToken);

                    if (rawTick != null)
                    {
                        var tick = new Tick(Symbol.Create(rawTick.Symbol), rawTick.Price, rawTick.Volume, rawTick.Timestamp, rawTick.Source);
                        await _channel.WriteAsync(tick, stoppingToken);
                        _metrics.TicksReceived.Add(1, new KeyValuePair<string, object?>("source", tick.Source));
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to poll REST endpoint for {Source}", source.Name);
            }
        }
    }

    private class ExchangeSource
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public double IntervalSeconds { get; set; }
        public string[]? Symbols { get; set; }
    }
}