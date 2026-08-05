using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Icbank.Platform.Application.Storage;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// <see cref="IObjectStorageReader"/> implementation backed by Azure Blob Storage, reached
/// exclusively through the API's managed identity (see <see cref="BlobServiceClient"/> registered
/// with <c>DefaultAzureCredential</c> in <c>DependencyInjection.AddSecurityServices</c> -- never a
/// storage account key). Selected when <c>ObjectStorage:Provider</c> is <c>AzureBlob</c>; the
/// filesystem implementation remains the default so local development and the test suite need no
/// cloud dependency.
/// </summary>
public sealed class AzureBlobObjectStorageReader : IObjectStorageReader
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>Initializes a new instance of the <see cref="AzureBlobObjectStorageReader"/> class.</summary>
    /// <param name="blobServiceClient">The managed-identity-authenticated Blob service client.</param>
    public AzureBlobObjectStorageReader(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <inheritdoc />
    public async Task<StoredObject?> OpenAsync(string normalizedRelativePath, CancellationToken cancellationToken)
    {
        (BlobContainerClient container, var blobName) = BlobPathResolver.Resolve(_blobServiceClient, normalizedRelativePath);
        BlobClient blobClient = container.GetBlobClient(blobName);

        try
        {
            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken);
            var contentType = response.Value.Details.ContentType;
            return new StoredObject(response.Value.Content.ToArray(), string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
