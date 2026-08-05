using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Icbank.Platform.Application.Storage;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// <see cref="IObjectStorageDeleter"/> implementation backed by Azure Blob Storage, reached
/// exclusively via the API's managed identity. Closes WAVE1-PORT-NOTES.md item 23 for deployed
/// environments the same way <see cref="FileSystemObjectStorageDeleter"/> does for local
/// development and the test suite.
/// </summary>
public sealed class AzureBlobObjectStorageDeleter : IObjectStorageDeleter
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>Initializes a new instance of the <see cref="AzureBlobObjectStorageDeleter"/> class.</summary>
    /// <param name="blobServiceClient">The managed-identity-authenticated Blob service client.</param>
    public AzureBlobObjectStorageDeleter(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string normalizedRelativePath, CancellationToken cancellationToken)
    {
        (BlobContainerClient container, var blobName) = BlobPathResolver.Resolve(_blobServiceClient, normalizedRelativePath);
        BlobClient blobClient = container.GetBlobClient(blobName);

        Azure.Response<bool> response = await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        return response.Value;
    }
}
