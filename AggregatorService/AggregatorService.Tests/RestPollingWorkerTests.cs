using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics.Metrics;

namespace AggregatorService.Tests.Services;

public class RestPollingWorkerTests
{
    private TradingMetrics CreateMetrics()
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("TestMeter"));
        return new TradingMetrics(meterFactory);
    }

    [Fact]
    public async Task RunPollingLoop_WhenExchangeReturns404_ShouldNotWriteToChannel()
    {
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var channel = Substitute.For<IngestionChannel>();

        // Настройка мока HttpClient для возврата 404
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "");
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        httpFactory.CreateClient("ExchangeClient").Returns(client);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                {"AggregatorSettings:Sources:0:Name", "TestEx"},
                {"AggregatorSettings:Sources:0:Type", "REST"},
                {"AggregatorSettings:Sources:0:Url", "api/ticker"},
                {"AggregatorSettings:Sources:0:IntervalSeconds", "0.1"},
                {"AggregatorSettings:AllowedSymbols:0", "BTCUSD"}
            }).Build();

        var sut = new RestPollingWorker(
            httpFactory,
            channel,
            CreateMetrics(),
            Substitute.For<ILogger<RestPollingWorker>>(),
            config);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        await sut.StartAsync(cts.Token);

        await channel.DidNotReceive().WriteAsync(Arg.Any<AggregatorService.ApiService.Domain.Models.Tick>(), Arg.Any<CancellationToken>());
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _content;
        public MockHttpMessageHandler(HttpStatusCode code, string content) { _code = code; _content = content; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_content) });
    }
}