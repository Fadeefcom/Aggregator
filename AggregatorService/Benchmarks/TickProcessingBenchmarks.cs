using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Services;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

[MemoryDiagnoser]
public class TickProcessingBenchmarks
{
    private TickProcessor _processor;
    private Tick _testTick;

    [GlobalSetup]
    public void Setup()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var alertChannel = new AlertChannel();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            {"AggregatorSettings:AllowedSymbols:0", "BTCUSD"}
        }).Build();

        _processor = new TickProcessor(cache, alertChannel, config, NullLogger<TickProcessor>.Instance);
        _testTick = new Tick(Symbol.Create("BTCUSD"), 60000m, 1.2m, DateTimeOffset.UtcNow, "Source");
    }

    [Benchmark]
    public void FullTickProcessingCycle()
    {
        var normalized = _processor.Normalize(_testTick);
        if (!_processor.IsDuplicate(normalized))
        {
            _processor.UpdateMetricsAndAggregate(normalized);
        }
    }

    [Benchmark]
    public void DeduplicationOnly()
    {
        _processor.IsDuplicate(_testTick);
    }
}
