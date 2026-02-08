namespace AggregatorService.ApiService.Application.DTOs;

public record AlertDto(string Symbol, string Message, DateTimeOffset Timestamp, string Severity);