using AggregatorService.ApiService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.ApiService.Controllers;

[ApiController]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly ISourceStatusRepository _statusRepository;
    private readonly ITickRepository _tickRepository;

    public MonitoringController(
        ISourceStatusRepository statusRepository,
        ITickRepository tickRepository)
    {
        _statusRepository = statusRepository;
        _tickRepository = tickRepository;
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetPerformanceReport(CancellationToken ct)
    {
        var sources = await _statusRepository.GetAllAsync(ct);
        var latestTickTime = await _tickRepository.GetLatestTickTimestampAsync(ct);

        var systemLag = latestTickTime.HasValue
            ? (DateTimeOffset.UtcNow - latestTickTime.Value).TotalMilliseconds
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