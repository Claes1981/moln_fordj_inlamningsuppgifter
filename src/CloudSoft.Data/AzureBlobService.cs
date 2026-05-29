using Azure.Identity;
using Azure.Storage.Blobs;
using CloudSoft.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudSoft.Data;

/// <summary>
/// Azure Blob Storage implementation using Managed Identity via DefaultAzureCredential.
/// Uploads file streams directly to blobs without buffering in memory.
/// </summary>
public class AzureBlobService : IBlobService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobService> _logger;

    public AzureBlobService(
        IConfiguration configuration,
        ILogger<AzureBlobService> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("BlobStorage");
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Connection string mode — used for local development
            _logger.LogInformation("AzureBlobService: Using connection string for blob storage.");
            var serviceClient = new BlobServiceClient(connectionString);
            _containerClient = serviceClient.GetBlobContainerClient("resumes");
        }
        else
        {
            // Managed Identity mode — used in production (Container Apps)
            var accountUrl = configuration.GetValue<string>("BlobStorage__AccountUrl")
                ?? throw new InvalidOperationException(
                    "Blob storage is not configured. Set 'BlobStorage__AccountUrl' for Managed Identity or 'ConnectionStrings:BlobStorage' for connection string.");

            _logger.LogInformation("AzureBlobService: Using Managed Identity for blob storage.");
            var credential = new DefaultAzureCredential();
            var serviceClient = new BlobServiceClient(new Uri(accountUrl), credential);
            _containerClient = serviceClient.GetBlobContainerClient("resumes");
        }
    }

    public async Task<string> UploadAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default)
    {
        // Use the default container ("resumes") — containerName param is for future extensibility
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken);
        _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}'.", blobName, containerName);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _containerClient.ExistsAsync(cancellationToken);
            return response.HasValue && response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage health check failed.");
            return false;
        }
    }
}
