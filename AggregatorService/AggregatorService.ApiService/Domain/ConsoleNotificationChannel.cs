namespace AggregatorService.ApiService.Domain.Alerts;

public class ConsoleNotificationChannel : INotificationChannel
{
    private readonly ILogger<ConsoleNotificationChannel> _logger;

    public ConsoleNotificationChannel(ILogger<ConsoleNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Alert alert, CancellationToken ct)
    {
        _logger.LogWarning("[ALERT CONSOLE] {Timestamp}: {Symbol} - {Message}", alert.Timestamp, alert.Symbol, alert.Message);
        return Task.CompletedTask;
    }
}

public class FileNotificationChannel : INotificationChannel
{
    private readonly string _filePath = "alerts.log";

    public async Task SendAsync(Alert alert, CancellationToken ct)
    {
        var line = $"{alert.Timestamp:u} [{alert.Severity}] {alert.Symbol}: {alert.Message}{Environment.NewLine}";
        await File.AppendAllTextAsync(_filePath, line, ct);
    }
}

public class EmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(ILogger<EmailNotificationChannel> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(Alert alert, CancellationToken ct)
    {
        // Stub: In production, use SmtpClient or SendGrid
        _logger.LogInformation("[EMAIL STUB] To: admin@trading.com, Subject: Alert {Symbol}, Body: {Message}", alert.Symbol, alert.Message);
        return Task.CompletedTask;
    }
}