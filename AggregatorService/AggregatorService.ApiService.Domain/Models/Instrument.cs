using AggregatorService.ApiService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace AggregatorService.ApiService.Domain.Models;

public class Instrument
{
    public Symbol Symbol { get; private set; }
    public string BaseCurrency { get; private set; }
    public string QuoteCurrency { get; private set; }
    public string Type { get; private set; }
    public bool IsActive { get; private set; }

    private Instrument() { }

    public Instrument(Symbol symbol, string baseCurrency, string quoteCurrency, string type = "Crypto")
    {
        Symbol = symbol;
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        Type = type;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
