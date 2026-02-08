using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics.Metrics;

namespace AggregatorService.Tests.Services;

public class WebSocketIngestionWorkerTests
{
    private TradingMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>())
            .Returns(new Meter("TestMeter"));
        return new TradingMetrics(meterFactory);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConnectionFails_ShouldRetryAfterDelay()
    {
        var channel = Substitute.For<IngestionChannel>();
        var processor = Substitute.For<ITickProcessor>();
        var logger = Substitute.For<ILogger<WebSocketIngestionWorker>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                {"AggregatorSettings:WebSocketUri", "ws://127.0.0.1:1"}
            }).Build();

        var sut = new WebSocketIngestionWorker(
            channel,
            processor,
            CreateMetrics(),
            logger,
            config);

        using var cts = new CancellationTokenSource();

        // Act
        await sut.StartAsync(cts.Token);

        bool errorLogged = false;
        for (int i = 0; i < 20; i++)
        {
            errorLogged = logger.ReceivedCalls().Any(call =>
                call.GetArguments().Length > 0 &&
                call.GetArguments()[0] is LogLevel level &&
                level == LogLevel.Error);

            if (errorLogged) break;
            await Task.Delay(100);
        }

        // Cleanup
        await sut.StopAsync(cts.Token);
        cts.Cancel();

        // Assert
        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}