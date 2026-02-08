using AggregatorService.ApiService.Application.Common;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Application.Services;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Infrastructure.Extensions;
using AggregatorService.ApiService.Infrastructure.Services;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.AspNetCore.HttpLogging;

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

builder.Services.AddScoped<INotificationChannel, ConsoleNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, FileNotificationChannel>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<RestPollingWorker>();
builder.Services.AddHostedService<WebSocketIngestionWorker>();
builder.Services.AddHostedService<AlertNotificationWorker>();

builder.Services.AddHttpClient("ExchangeClient", client =>
{
    client.BaseAddress = new Uri("http://loadgenerator");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();