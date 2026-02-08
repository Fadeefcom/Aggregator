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
builder.Services.AddHostedService<TickProcessingService>();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
        db.Database.EnsureCreated();
    }
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
