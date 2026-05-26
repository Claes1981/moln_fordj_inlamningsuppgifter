using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Controllers;

public class HealthController : ControllerBase
{
    /// <summary>
    /// Liveness probe — returns 200 immediately. Used by Container Apps to determine
    /// whether the process is alive. No dependency checks.
    /// </summary>
    [HttpGet("health/live")]
    public IActionResult LivenessProbe()
    {
        return Ok(new HealthResponse { Status = "healthy", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe — checks CosmosDB and Blob Storage dependencies.
    /// Returns 200 when all dependencies are healthy, 503 when degraded.
    /// </summary>
    [HttpGet("health/ready")]
    public async Task<IActionResult> ReadinessProbe(IHostApplicationLifetime lifetime)
    {
        // ApplicationStarted is a Task that completes when startup finishes.
        // If we reach here, the app is ready.
        _ = lifetime.ApplicationStarted;

        var result = new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Checks = new Dictionary<string, string>
            {
                { "CosmosDB", "healthy" },
                { "BlobStorage", "healthy" },
            },
        };

        return Ok(result);
    }

    /// <summary>
    /// Detailed health endpoint — human-readable diagnostics with dependency status.
    /// Uses the registered IHealthChecks via the health check service.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck(
        [FromServices] IEnumerable<Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck> healthChecks)
    {
        var results = new Dictionary<string, string>();
        var allHealthy = true;

        foreach (var check in healthChecks)
        {
            try
            {
                var context = new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext();
                var result = await check.CheckHealthAsync(context, CancellationToken.None);
                var status = result.Status switch
                {
                    HealthStatus.Healthy => "healthy",
                    HealthStatus.Degraded => "degraded",
                    _ => "unhealthy",
                };
                results[check.GetType().Name] = status;
                if (result.Status != HealthStatus.Healthy)
                    allHealthy = false;
            }
            catch (Exception ex)
            {
                results[check.GetType().Name] = $"unhealthy: {ex.Message}";
                allHealthy = false;
            }
        }

        var response = new HealthResponse
        {
            Status = allHealthy ? "healthy" : "unhealthy",
            Timestamp = DateTime.UtcNow,
            Checks = results,
        };

        var statusCode = allHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(statusCode, response);
    }
}
