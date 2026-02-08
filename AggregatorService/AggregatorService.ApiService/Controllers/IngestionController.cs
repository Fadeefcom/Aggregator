using AggregatorService.ApiService.Data;
using AggregatorService.ApiService.Services;
using AggregatorService.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionChannel _channel;
    private readonly TradingMetrics _metrics;

    public IngestionController(IngestionChannel channel, TradingMetrics metrics)
    {
        _channel = channel;
        _metrics = metrics;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] Tick tick)
    {
        await _channel.WriteAsync(tick);
        _metrics.TicksReceived.Add(1, new KeyValuePair<string, object?>("source", tick.Source));
        return Accepted();
    }
}