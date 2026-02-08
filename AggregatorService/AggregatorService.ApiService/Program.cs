using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddSingleton<IngestionChannel>();

builder.Services.AddHttpClient("ExchangeClient", client =>
{
    client.BaseAddress = new Uri("http://loadgenerator");
});

builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<RestPollingWorker>();
builder.Services.AddHostedService<WebSocketIngestionWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        db.Database.EnsureCreated();
    }
}

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();