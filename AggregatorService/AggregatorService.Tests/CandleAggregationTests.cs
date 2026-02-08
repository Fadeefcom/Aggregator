using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.Services;
using AggregatorService.ApiService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AggregatorService.Tests.Domain;

public class CandleAggregationTests
{
    private readonly Symbol _symbol = Symbol.Create("BTCUSD");
    private readonly DateTimeOffset _baseTime = new(2026, 2, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CandleBuilder_ShouldCalculateCorrectOHLCV()
    {
        // Arrange
        var builder = new CandleBuilder(_symbol, _baseTime, TimeSpan.FromMinutes(1));

        var ticks = new[]
        {
            new Tick(_symbol, 60000m, 1.0m, _baseTime.AddSeconds(5), "ex"),
            new Tick(_symbol, 60100m, 2.0m, _baseTime.AddSeconds(10), "ex"),
            new Tick(_symbol, 59900m, 0.5m, _baseTime.AddSeconds(15), "ex"),
            new Tick(_symbol, 60050m, 1.5m, _baseTime.AddSeconds(20), "ex")
        };

        // Act
        foreach (var tick in ticks) builder.AddTick(tick);
        var candle = builder.Build();

        // Assert
        candle.Open.Should().Be(60000m);
        candle.High.Should().Be(60100m);
        candle.Low.Should().Be(59900m);
        candle.Close.Should().Be(60050m);
        candle.TotalVolume.Should().Be(5.0m);
    }

    [Fact]
    public void CandleBuilder_AveragePrice_ShouldBeVolumeWeighted()
    {
        // Arrange
        var builder = new CandleBuilder(_symbol, _baseTime, TimeSpan.FromMinutes(1));
        // (10 * 100) + (20 * 200) = 1000 + 4000 = 5000. Total Volume = 30. Avg = 5000 / 30 = 166.66...
        builder.AddTick(new Tick(_symbol, 100m, 10m, _baseTime, "ex"));
        builder.AddTick(new Tick(_symbol, 200m, 20m, _baseTime, "ex"));

        // Act
        var candle = builder.Build();

        // Assert
        candle.AveragePrice.Should().BeApproximately(166.66666666666666666666666667m, 0.0000000001m);
    }

    [Fact]
    public void CandleBuilder_Volatility_ShouldCalculateStandardDeviation()
    {
        // Arrange
        var builder = new CandleBuilder(_symbol, _baseTime, TimeSpan.FromMinutes(1));
        // Prices: 10, 20. Mean: 15. Variance: ((10-15)^2 + (20-15)^2)/2 = (25 + 25)/2 = 25. SD = 5.
        builder.AddTick(new Tick(_symbol, 10m, 1m, _baseTime, "ex"));
        builder.AddTick(new Tick(_symbol, 20m, 1m, _baseTime, "ex"));

        // Act
        var candle = builder.Build();

        // Assert
        candle.Volatility.Should().Be(5m);
    }

    [Fact]
    public void CandleAggregatorService_ShouldCloseCandle_WhenTickIsOutsideTimeframe()
    {
        // Arrange
        var timeframes = new[] { TimeSpan.FromMinutes(1) };
        var service = new CandleAggregatorService(timeframes);

        var firstTick = new Tick(_symbol, 60000m, 1m, _baseTime, "ex");
        var secondTick = new Tick(_symbol, 61000m, 1m, _baseTime.AddMinutes(1).AddSeconds(1), "ex");

        // Act
        service.Aggregate(firstTick).Should().BeEmpty(); // Свеча еще открыта
        var closedCandles = service.Aggregate(secondTick).ToList(); // Тик вне интервала должен закрыть предыдущую

        // Assert
        closedCandles.Should().HaveCount(1);
        closedCandles[0].Close.Should().Be(60000m);
        closedCandles[0].OpenTime.Should().Be(_baseTime);
    }

    [Theory]
    [InlineData(1, 0, 0)]  // 12:00:05 -> 12:00:00 для 1m
    [InlineData(5, 0, 0)]  // 12:04:59 -> 12:00:00 для 5m
    [InlineData(5, 7, 5)]  // 12:07:00 -> 12:05:00 для 5m
    [InlineData(60, 45, 0)] // 12:45:00 -> 12:00:00 для 1h
    public void CandleAggregatorService_ShouldAlignToCorrectBucketTime(int periodMin, int tickMin, int expectedBucketMin)
    {
        // Arrange
        var period = TimeSpan.FromMinutes(periodMin);
        var service = new CandleAggregatorService(new[] { period });
        var tickTime = new DateTimeOffset(2026, 2, 8, 12, tickMin, 30, TimeSpan.Zero);
        var tick = new Tick(_symbol, 100m, 1m, tickTime, "ex");

        // Act
        service.Aggregate(tick);

        // Используем Reflection для проверки внутреннего состояния или проверяем через закрытие
        var closingTick = new Tick(_symbol, 100m, 1m, tickTime.Add(period).AddMinutes(1), "ex");
        var result = service.Aggregate(closingTick).First();

        // Assert
        result.OpenTime.Minute.Should().Be(expectedBucketMin);
        result.Period.Should().Be(period);
    }
}