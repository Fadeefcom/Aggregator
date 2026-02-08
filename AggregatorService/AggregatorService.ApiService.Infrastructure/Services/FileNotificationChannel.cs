using AggregatorService.ApiService.Application.Interfaces;
using AggregatorService.ApiService.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace AggregatorService.ApiService.Infrastructure.Services;

public class FileNotificationChannel : INotificationChannel
{
    private readonly string _filePath;

    public FileNotificationChannel(IConfiguration configuration)
    {
        _filePath = configuration["AggregatorSettings:AlertFilePath"] ?? "alerts.log";
    }

    public async Task SendAsync(Alert alert, CancellationToken ct)
    {
        var line = $"{alert.Timestamp:u} [{alert.Severity}] {alert.Symbol}: {alert.Message}{Environment.NewLine}";
        await File.AppendAllTextAsync(_filePath, line, ct);
    }
}