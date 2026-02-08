using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace AggregatorService.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionChannel _channel;
    private readonly Counter<long> _receivedCounter;

    public IngestionController(IngestionChannel channel, IMeterFactory meterFactory)
    {
        _channel = channel;
        var meter = meterFactory.Create("TradingSystem.Aggregator");
        _receivedCounter = meter.CreateCounter<long>("ticks_received");
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] Tick tick)
    {
        await _channel.WriteAsync(tick);
        _receivedCounter.Add(1);
        return Accepted();
    }
}
