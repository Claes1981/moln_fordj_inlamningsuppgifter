using CloudSoft.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloudSoft.Data;

/// <summary>
/// No-operation blob service for local development when Azure Blob Storage is not configured.
/// All operations succeed silently. Health checks return false.
/// </summary>
public class NoOpBlobService : IBlobService
{
    public Task<string> UploadAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default)
    {
        // In no-op mode, return a fake URL
        return Task.FromResult($"https://local-dev/{containerName}/{blobName}");
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Not available in no-op mode
        return Task.FromResult(false);
    }
}
