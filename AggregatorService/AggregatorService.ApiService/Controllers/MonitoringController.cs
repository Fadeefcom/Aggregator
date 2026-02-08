using AggregatorService.ApiService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AggregatorService.ApiService.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly TradingDbContext _dbContext;

    public MonitoringController(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetPerformanceReport()
    {
        var sources = await _dbContext.SourceStatuses.AsNoTracking().ToListAsync();

        var latestTick = await _dbContext.Ticks
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new { t.Timestamp, t.Source })
            .FirstOrDefaultAsync();

        var systemLag = latestTick != null
            ? (DateTimeOffset.UtcNow - latestTick.Timestamp).TotalMilliseconds
            : 0;

        var report = new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            SystemStatus = systemLag > 1000 ? "Degraded" : "Healthy",
            GlobalLagMs = systemLag,
            Sources = sources.Select(s => new
            {
                s.SourceName,
                Status = s.IsOnline ? "Online" : "Offline",
                LastSeen = s.LastUpdate,
                TotalTicks = s.TicksCount,
                SecondsSinceLastUpdate = (DateTimeOffset.UtcNow - s.LastUpdate).TotalSeconds
            })
        };

        return Ok(report);
    }
}