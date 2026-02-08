using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics.Metrics;
using Xunit;

namespace AggregatorService.Tests.Services;

public class TickProcessingServiceTests
{
    private readonly IngestionChannel _channel;
    private readonly ITickProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITickRepository _tickRepo;
    private readonly IUnitOfWork _uow;

    public TickProcessingServiceTests()
    {
        _channel = new IngestionChannel();
        _processor = Substitute.For<ITickProcessor>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();

        var scope = Substitute.For<IServiceScope>();
        _uow = Substitute.For<IUnitOfWork>();
        _tickRepo = Substitute.For<ITickRepository>();

        scope.ServiceProvider.GetService(typeof(IUnitOfWork)).Returns(_uow);
        scope.ServiceProvider.GetService(typeof(ITickRepository)).Returns(_tickRepo);
        scope.ServiceProvider.GetService(typeof(ICandleRepository)).Returns(Substitute.For<ICandleRepository>());
        scope.ServiceProvider.GetService(typeof(ISourceStatusRepository)).Returns(Substitute.For<ISourceStatusRepository>());
        _scopeFactory.CreateScope().Returns(scope);
    }

    private TradingMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("TestMeter"));
        return new TradingMetrics(meterFactory);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessAndSaveData()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                {"AggregatorSettings:BatchSize", "1"}
            }).Build();

        var sut = new TickProcessingService(_channel, _processor, _scopeFactory, CreateMetrics(), config, Substitute.For<ILogger<TickProcessingService>>());

        var tick = new Tick(Symbol.Create("BTCUSD"), 60000, 1, DateTimeOffset.UtcNow, "test");
        _processor.ShouldProcess(tick).Returns(true);
        _processor.Normalize(tick).Returns(tick);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        await _channel.WriteAsync(tick);
        await Task.Delay(100);

        await _tickRepo.Received().AddBatchAsync(Arg.Any<IEnumerable<Tick>>(), Arg.Any<CancellationToken>());
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}