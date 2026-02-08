using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Infrastructure.Persistence;
using AggregatorService.ApiService.Infrastructure.Persistence.Repositories;
using AggregatorService.ApiService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AggregatorService.ApiService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITickRepository, TickRepository>();
        services.AddScoped<ICandleRepository, CandleRepository>();
        services.AddScoped<ISourceStatusRepository, SourceStatusRepository>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddMemoryCache();
        services.AddSingleton<ITickDeduplicator, InMemoryTickDeduplicator>();

        return services;
    }
}
