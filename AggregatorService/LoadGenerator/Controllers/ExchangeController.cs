using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LoadGenerator.Controllers;

[ApiController]
[Route("api/exchange")]
public class ExchangeController : ControllerBase
{
    [HttpGet("ticker/{symbol}")]
    public IActionResult GetTicker(string symbol)
    {
        var tick = new
        {
            Symbol = symbol,
            Price = (decimal)Random.Shared.Next(60000, 65000),
            Volume = (decimal)Random.Shared.NextDouble() * 10,
            Timestamp = DateTimeOffset.UtcNow,
            Source = "REST_BINANCE"
        };

        return Ok(tick);
    }

    [Route("/ws/stream")]
    public async Task GetStream()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await StreamMarketData(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task StreamMarketData(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var tick = new
            {
                Symbol = "BTCUSD",
                Price = (decimal)Random.Shared.Next(60000, 65000),
                Volume = (decimal)Random.Shared.NextDouble() * 5,
                Timestamp = DateTimeOffset.UtcNow,
                Source = "WS_KRAKEN"
            };

            var json = JsonSerializer.Serialize(tick);
            var bytes = Encoding.UTF8.GetBytes(json);

            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            await Task.Delay(50);
        }
    }
}