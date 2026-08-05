using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Icbank.Platform.Application.Storage;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// <see cref="IObjectStorageWriter"/> implementation backed by Azure Blob Storage, reached
/// exclusively via the API's managed identity. Selected when <c>ObjectStorage:Provider</c> is
/// <c>AzureBlob</c>; the filesystem implementation remains the default for local development and
/// the test suite.
/// </summary>
public sealed class AzureBlobObjectStorageWriter : IObjectStorageWriter
{
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>Initializes a new instance of the <see cref="AzureBlobObjectStorageWriter"/> class.</summary>
    /// <param name="blobServiceClient">The managed-identity-authenticated Blob service client.</param>
    public AzureBlobObjectStorageWriter(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(string folderPrefix, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var safeExtension = ContentTypeExtensions.Resolve(contentType);
        var relativePath = $"{folderPrefix.TrimEnd('/')}/{Guid.NewGuid():N}{safeExtension}";

        (BlobContainerClient container, string blobName) = BlobPathResolver.Resolve(_blobServiceClient, relativePath);
        BlobClient blobClient = container.GetBlobClient(blobName);

        using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return relativePath;
    }
}
