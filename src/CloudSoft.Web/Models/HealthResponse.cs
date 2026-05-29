namespace CloudSoft.Web.Models;

/// <summary>Response model for the health check endpoint.</summary>
public sealed class HealthResponse
{
    public string Status { get; init; } = "healthy";
    public DateTime Timestamp { get; init; }
    public Dictionary<string, string>? Checks { get; init; }
}
