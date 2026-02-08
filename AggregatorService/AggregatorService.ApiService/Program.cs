using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Application.Services;
using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Infrastructure.Extensions;
using AggregatorService.ApiService.Infrastructure.Services;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;
    logging.RequestHeaders.Add("x-request-id");
    logging.ResponseHeaders.Add("x-request-id");
    logging.MediaTypeOptions.AddText("application/json");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<IngestionChannel>();
builder.Services.AddSingleton<AlertChannel>();
builder.Services.AddSingleton<TradingMetrics>();
builder.Services.AddSingleton<ITickProcessor, TickProcessor>();

builder.Services.AddSingleton<INotificationChannel, ConsoleNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, FileNotificationChannel>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<RestPollingWorker>();
builder.Services.AddHostedService<WebSocketIngestionWorker>();
builder.Services.AddHostedService<AlertNotificationWorker>();

builder.Services.AddHttpClient("ExchangeClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5098");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TradingDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();