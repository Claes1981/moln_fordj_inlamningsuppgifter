using System.Diagnostics;

namespace CloudSoft.Web.Middleware;

/// <summary>
/// Middleware that generates a correlation ID for each request and adds it to the logging scope.
/// Enables tracing a single request across all log entries in Log Analytics.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // Reuse existing trace ID if present (e.g. from upstream services), otherwise generate one
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        // Echo it back in the response header for client-side tracing
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Add correlation ID to the logging scope so all structured logs include it
        using (logger.BeginScope(new KeyValuePair<string, object>("CorrelationId", correlationId)))
        {
            await _next(context);
        }
    }
}
