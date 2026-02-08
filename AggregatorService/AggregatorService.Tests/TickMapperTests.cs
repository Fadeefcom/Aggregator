using AggregatorService.ApiService.Application.DTOs;
using FluentAssertions;

namespace AggregatorService.Tests.Application;

public class TickMapperTests
{
    [Fact]
    public void ToDomain_WithValidDto_ShouldMapCorrectly()
    {
        var dto = new TickDto
        {
            Symbol = "BTCUSD",
            Price = 60000.5m,
            Volume = 1.2m,
            Timestamp = DateTimeOffset.UtcNow,
            Source = "Binance"
        };

        var result = dto.ToDomain();

        result.Should().NotBeNull();
        result.Symbol.Value.Should().Be(dto.Symbol);
        result.Price.Should().Be(dto.Price);
        result.Volume.Should().Be(dto.Volume);
        result.Source.Should().Be(dto.Source);
        result.Id.Should().NotBeEmpty();
    }
}