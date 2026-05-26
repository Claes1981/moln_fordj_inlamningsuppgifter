using CloudSoft.Domain;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudSoft.Web.HealthChecks;

/// <summary>
/// Deep health check for Azure Blob Storage connectivity.
/// Uses IBlobService.IsAvailableAsync to verify the service is reachable.
/// </summary>
public class BlobHealthCheck : IHealthCheck
{
    private readonly IBlobService _blobService;

    public BlobHealthCheck(IBlobService blobService)
    {
        _blobService = blobService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var available = await _blobService.IsAvailableAsync(linkedToken.Token);
            return available
                ? HealthCheckResult.Healthy("Blob Storage is available.")
                : HealthCheckResult.Degraded("Blob Storage returned false from ExistsAsync.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await CheckHealthAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob Storage is not available.", ex);
        }
    }
}
