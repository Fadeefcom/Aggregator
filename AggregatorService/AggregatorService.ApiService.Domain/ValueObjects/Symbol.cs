namespace AggregatorService.ApiService.Domain.ValueObjects;

public record Symbol
{
    public string Value { get; }

    private Symbol(string value) => Value = value;

    public static Symbol Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Symbol cannot be empty");
        return new Symbol(value.ToUpperInvariant());
    }

    public static implicit operator string(Symbol s) => s.Value;
}
