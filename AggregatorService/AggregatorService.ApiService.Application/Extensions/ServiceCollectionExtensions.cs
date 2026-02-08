using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Application.Services;
using AggregatorService.ApiService.Domain.Services;
using AggregatorService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AggregatorService.ApiService.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<CandleAggregatorService>();

        services.AddScoped<ITickIngestionService, TickIngestionService>();
        services.AddScoped<IAlertService, AlertService>();

        return services;
    }
}