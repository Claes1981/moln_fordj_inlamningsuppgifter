namespace CloudSoft.Web.Middleware;

/// <summary>
/// Middleware that validates an API key passed in the "X-API-Key" header.
/// Returns 401 (Unauthorized) if missing, 403 (Forbidden) if invalid.
/// The expected key is read from configuration (ApiKey__Value).
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        // Use double-underscore convention for nested config sections via environment variables
        _apiKey = configuration.GetValue<string>("ApiKey__Value") ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply API key validation to /api/* routes
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip Swagger UI and OpenAPI JSON endpoints
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/openapi"))
        {
            await _next(context);
            return;
        }

        var providedKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey))
        {
            context.Response.Headers.Append("WWW-Authenticate", "ApiKey");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is missing. Provide it via the 'X-API-Key' header.");
            return;
        }

        if (!string.Equals(providedKey, _apiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await _next(context);
    }
}
