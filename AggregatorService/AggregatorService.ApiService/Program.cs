using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain;
using AggregatorService.ApiService.Domain.Alerts;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddSingleton<IngestionChannel>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TradingMetrics>();

builder.Services.AddSingleton<AlertChannel>();
builder.Services.AddSingleton<INotificationChannel, ConsoleNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, FileNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddHostedService<AlertNotificationWorker>();

builder.Services.AddSingleton<ITickProcessor, TickProcessor>();

builder.Services.AddHttpClient("ExchangeClient", client =>
{
    client.BaseAddress = new Uri("http://loadgenerator");
});

builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<RestPollingWorker>();
builder.Services.AddHostedService<WebSocketIngestionWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();