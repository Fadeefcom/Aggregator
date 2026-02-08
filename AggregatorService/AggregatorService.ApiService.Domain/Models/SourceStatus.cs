using System.ComponentModel.DataAnnotations;

namespace AggregatorService.ApiService.Domain.Models;

public class SourceStatus
{
    public string SourceName { get; private set; }
    public bool IsOnline { get; private set; }
    public DateTimeOffset LastUpdate { get; private set; }
    public long TicksCount { get; private set; }
    public string? LastError { get; private set; }

    private SourceStatus() { }

    public SourceStatus(string sourceName)
    {
        SourceName = sourceName;
        IsOnline = true;
        LastUpdate = DateTimeOffset.UtcNow;
        TicksCount = 0;
    }

    public void UpdateHeartbeat(bool isOnline, string? error = null)
    {
        IsOnline = isOnline;
        LastUpdate = DateTimeOffset.UtcNow;
        if (error != null) LastError = error;
    }

    public void IncrementTickCount()
    {
        TicksCount++;
        LastUpdate = DateTimeOffset.UtcNow;
        IsOnline = true;
        LastError = null;
    }
}