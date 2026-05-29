using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudSoft.Web.HealthChecks;

/// <summary>
/// Deep health check for CosmosDB connectivity.
/// Performs a lightweight ReadContainerAsync call to verify the service is reachable.
/// </summary>
public class CosmosHealthCheck : IHealthCheck
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;

    public CosmosHealthCheck(
        CosmosClient cosmosClient,
        IConfiguration configuration)
    {
        _cosmosClient = cosmosClient;
        _databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? CloudSoft.Domain.Constants.DefaultDatabaseName;
        _containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? CloudSoft.Domain.Constants.DefaultContainerName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // 5-second timeout for health check
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            await container.ReadContainerAsync(new Microsoft.Azure.Cosmos.ContainerRequestOptions(), linkedToken.Token);

            return HealthCheckResult.Healthy("CosmosDB is available.");
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("CosmosDB health check timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CosmosDB is not available.", ex);
        }
    }
}
