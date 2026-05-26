namespace CloudSoft.Domain;

/// <summary>
/// Abstraction for blob storage operations. Allows swapping Azure Blob Storage
/// with a local/dev implementation or a test mock.
/// </summary>
public interface IBlobService
{
    /// <summary>
    /// Uploads the file stream to a blob container.
    /// </summary>
    Task<string> UploadAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the blob service is available (for health probes).
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
