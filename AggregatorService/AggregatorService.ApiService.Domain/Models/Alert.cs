using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Domain.Models;

public record Alert(Symbol Symbol, string Message, DateTimeOffset Timestamp, string Severity);
