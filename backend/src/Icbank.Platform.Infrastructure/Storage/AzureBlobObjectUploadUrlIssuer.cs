using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Icbank.Platform.Application.Storage;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// <see cref="IObjectUploadUrlIssuer"/> implementation backed by Azure Blob Storage user-delegation
/// SAS tokens (BUSINESS-RULES.md §12.3's client-PUTs-directly presigned-URL flow). A user
/// delegation key is requested from the Blob service using the API's own managed identity
/// credentials -- no storage account key is ever generated, stored, or needed. The resulting SAS
/// is short-lived (<see cref="AzureBlobStorageOptions.UploadUrlLifetimeMinutes"/>) and scoped to a
/// single blob with write-only permission.
/// </summary>
public sealed class AzureBlobObjectUploadUrlIssuer : IObjectUploadUrlIssuer
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="AzureBlobObjectUploadUrlIssuer"/> class.</summary>
    /// <param name="blobServiceClient">The managed-identity-authenticated Blob service client.</param>
    /// <param name="options">The bound Azure Blob storage configuration.</param>
    public AzureBlobObjectUploadUrlIssuer(BlobServiceClient blobServiceClient, IOptions<AzureBlobStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<PresignedUpload> IssueAsync(string folderPrefix, string fileName, string? contentType, CancellationToken cancellationToken)
    {
        var safeExtension = Path.GetExtension(fileName);
        var objectPath = $"{folderPrefix.TrimEnd('/')}/{Guid.NewGuid():N}{safeExtension}";

        (BlobContainerClient container, string blobName) = BlobPathResolver.Resolve(_blobServiceClient, objectPath);
        BlobClient blobClient = container.GetBlobClient(blobName);

        var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5); // Why: tolerate minor clock skew between the API host and Azure Storage.
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.UploadUrlLifetimeMinutes);

        UserDelegationKey userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = startsOn,
            ExpiresOn = expiresOn,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uriBuilder = new BlobUriBuilder(blobClient.Uri)
        {
            Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, _options.AccountName),
        };

        return new PresignedUpload(uriBuilder.ToUri().ToString(), objectPath);
    }
}
