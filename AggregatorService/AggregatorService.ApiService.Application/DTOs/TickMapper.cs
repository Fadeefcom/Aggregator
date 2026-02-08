using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.ValueObjects;

namespace AggregatorService.ApiService.Application.DTOs;

public static class TickMapper
{
    public static Tick ToDomain(this TickDto dto)
    {
        return new Tick(
            Symbol.Create(dto.Symbol),
            dto.Price,
            dto.Volume,
            dto.Timestamp,
            dto.Source
        );
    }
}