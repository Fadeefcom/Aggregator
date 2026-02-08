using Microsoft.AspNetCore.Mvc;
using LoadGenerator.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace LoadGenerator.Controllers;

[ApiController]
public class ExchangeController : ControllerBase
{
    private readonly MarketSimulationService _simulation;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(MarketSimulationService simulation, ILogger<ExchangeController> logger)
    {
        _simulation = simulation;
        _logger = logger;
    }

    [HttpGet("api/{exchangeId}/ticker/{symbol}")]
    public IActionResult GetTicker(string exchangeId, string symbol)
    {
        var profile = _simulation.GetProfile(exchangeId);

        // Проверка: торгуется ли этот символ на этой бирже?
        if (!profile.SupportedSymbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
        {
            return NotFound($"Symbol {symbol} not found on {exchangeId}");
        }

        var tick = GenerateRandomTick(profile, symbol);
        return Ok(tick);
    }

    // Маршрут: ws/exchange-1/stream
    [Route("ws/{exchangeId}/stream")]
    public async Task GetStream(string exchangeId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var profile = _simulation.GetProfile(exchangeId);
            _logger.LogInformation("WebSocket connected to mock exchange: {Exchange}", exchangeId);
            await StreamMarketData(webSocket, profile);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task StreamMarketData(WebSocket webSocket, ExchangeProfile profile)
    {
        var buffer = new byte[1024 * 4];
        var random = Random.Shared;

        while (webSocket.State == WebSocketState.Open)
        {
            // Выбираем случайный символ из доступных на этой бирже
            var symbol = profile.SupportedSymbols[random.Next(profile.SupportedSymbols.Length)];

            var tick = GenerateRandomTick(profile, symbol);
            var json = JsonSerializer.Serialize(tick);
            var bytes = Encoding.UTF8.GetBytes(json);

            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            // Имитируем разную скорость поступления данных
            await Task.Delay(random.Next(50, 500));
        }
    }

    private object GenerateRandomTick(ExchangeProfile profile, string symbol)
    {
        var basePrice = symbol.StartsWith("BTC") ? 60000 :
                        symbol.StartsWith("ETH") ? 3000 :
                        symbol.StartsWith("SOL") ? 150 : 100;

        return new
        {
            Symbol = symbol.ToUpper(),
            Price = (decimal)(basePrice + profile.BasePriceOffset + (decimal)(Random.Shared.NextDouble() * 100 - 50)),
            Volume = (decimal)Random.Shared.NextDouble() * 5,
            Timestamp = DateTimeOffset.UtcNow,
            Source = profile.Name // Возвращаем имя биржи как Source
        };
    }
}