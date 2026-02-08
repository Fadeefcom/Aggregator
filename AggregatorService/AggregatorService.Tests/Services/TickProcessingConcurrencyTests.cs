using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using System.Diagnostics.Metrics;

namespace AggregatorService.Tests.Services;

public class TickProcessingConcurrencyTests
{
    private readonly IngestionChannel _channel = new();
    private readonly ITickProcessor _processor = Substitute.For<ITickProcessor>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly ITickRepository _tickRepo = Substitute.For<ITickRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public TickProcessingConcurrencyTests()
    {
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IUnitOfWork)).Returns(_uow);
        scope.ServiceProvider.GetService(typeof(ITickRepository)).Returns(_tickRepo);
        scope.ServiceProvider.GetService(typeof(ICandleRepository)).Returns(Substitute.For<ICandleRepository>());
        scope.ServiceProvider.GetService(typeof(ISourceStatusRepository)).Returns(Substitute.For<ISourceStatusRepository>());
        _scopeFactory.CreateScope().Returns(scope);

        _processor.ShouldProcess(Arg.Any<Tick>()).Returns(true);
        _processor.Normalize(Arg.Any<Tick>()).Returns(x => x[0]);
    }

    private TradingMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("TestMeter"));
        return new TradingMetrics(meterFactory);
    }

    [Fact]
    public async Task ExecuteAsync_UnderHighLoad_ShouldProcessAllTicks()
    {
        var totalTicks = 2000;
        var batchSize = 500;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                {"AggregatorSettings:BatchSize", batchSize.ToString()}
            }).Build();

        var sut = new TickProcessingService(_channel, _processor, _scopeFactory, CreateMetrics(), config, Substitute.For<ILogger<TickProcessingService>>());

        using var cts = new CancellationTokenSource();
        var serviceTask = sut.StartAsync(cts.Token);

        var producers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < totalTicks / 4; i++)
            {
                await _channel.WriteAsync(new Tick(Symbol.Create("BTCUSD"), 60000, 1, DateTimeOffset.UtcNow, "source"));
            }
        }));

        await Task.WhenAll(producers);
        await Task.Delay(500);

        await sut.StopAsync(cts.Token);

        await _tickRepo.Received(Quantity.AtLeastOne())
            .AddBatchAsync(Arg.Any<IEnumerable<Tick>>(), Arg.Any<CancellationToken>());
        await _uow.Received(Quantity.AtLeastOne())
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}