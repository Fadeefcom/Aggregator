using AggregatorService.ApiService.Domain.ValueObjects;
using FluentAssertions;

namespace AggregatorService.Tests.Domain;

public class SymbolTests
{
    [Theory]
    [InlineData("btcusd", "BTCUSD")]
    [InlineData("EthUsd", "ETHUSD")]
    [InlineData("SOLUSD", "SOLUSD")]
    public void Create_WithValidValue_ShouldReturnUpperCaseSymbol(string input, string expected)
    {
        var symbol = Symbol.Create(input);

        symbol.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidValue_ShouldThrowArgumentException(string input)
    {
        Action act = () => Symbol.Create(input);

        act.Should().Throw<ArgumentException>();
    }
}