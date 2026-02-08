using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Domain;
using AggregatorService.ApiService.Domain.Alerts; // Added
using AggregatorService.ApiService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ... (DB Context setup remains same) ...
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Core Services
builder.Services.AddControllers();
builder.Services.AddSingleton<IngestionChannel>();
builder.Services.AddMemoryCache();

// --- NEW: Alerting Subsystem ---
builder.Services.AddSingleton<AlertChannel>();
builder.Services.AddSingleton<INotificationChannel, ConsoleNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, FileNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddHostedService<AlertNotificationWorker>();
// -------------------------------

builder.Services.AddSingleton<ITickProcessor, TickProcessor>();

builder.Services.AddHttpClient("ExchangeClient", client =>
{
    client.BaseAddress = new Uri("http://loadgenerator");
});

// Workers
builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<RestPollingWorker>();
builder.Services.AddHostedService<WebSocketIngestionWorker>();

var app = builder.Build();

// ... (Rest of Program.cs remains same) ...

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();