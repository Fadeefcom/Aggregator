using AggregatorService.ApiService.Application.DTOs;
using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using AggregatorService.ApiService.Domain.Rules;
using AggregatorService.ApiService.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AggregatorService.Application.Services;

public class AlertService : IAlertService
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AlertService> _logger;
    private readonly ConcurrentDictionary<string, Tick> _lastTicks = new();
    private readonly List<IAlertRule> _rules = new();

    public AlertService(
        INotificationService notificationService,
        IConfiguration configuration,
        ILogger<AlertService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;

        InitializeRules(configuration);
    }

    private void InitializeRules(IConfiguration configuration)
    {
        var rulesSection = configuration.GetSection("Alerting:Rules");
        foreach (var ruleConfig in rulesSection.GetChildren())
        {
            var type = ruleConfig["Type"];
            if (string.Equals(type, "Price", StringComparison.OrdinalIgnoreCase))
            {
                var symbolStr = ruleConfig["Symbol"];
                if (!string.IsNullOrEmpty(symbolStr))
                {
                    var symbol = Symbol.Create(symbolStr);
                    var min = ruleConfig.GetValue<decimal>("MinPrice");
                    var max = ruleConfig.GetValue<decimal>("MaxPrice");
                    _rules.Add(new PriceThresholdRule(symbol, min, max));
                }
            }
            else if (string.Equals(type, "Volume", StringComparison.OrdinalIgnoreCase))
            {
                var multiplier = ruleConfig.GetValue<decimal>("Multiplier");
                _rules.Add(new VolumeSpikeRule(multiplier));
            }
        }
    }

    public async Task CheckAlertsAsync(Tick tick, CancellationToken ct = default)
    {
        _lastTicks.TryGetValue(tick.Symbol.Value, out var previousTick);

        foreach (var rule in _rules)
        {
            if (rule.Evaluate(tick, previousTick, out var reason))
            {
                var alert = new AlertDto(
                    tick.Symbol.Value,
                    reason,
                    tick.Timestamp,
                    "Warning");

                _logger.LogWarning("Alert triggered: {Reason}", reason);
                await _notificationService.SendAlertAsync(alert, ct);
            }
        }

        _lastTicks[tick.Symbol.Value] = tick;
    }
}