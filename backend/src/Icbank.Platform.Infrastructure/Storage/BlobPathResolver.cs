using Azure.Storage.Blobs;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// Maps this codebase's folder-prefix object-path convention (e.g. <c>weekend/&lt;guid&gt;.png</c>,
/// <c>designs/generated/&lt;guid&gt;.png</c>) onto the five pre-created Azure Blob containers
/// (<c>infra/modules/storage.bicep</c>: weekend, designs, shorfah, media-reports, ai-year), so the
/// leading path segment becomes the container name and the remainder becomes the blob name within
/// it. Kept as a single shared resolver so every Azure Blob adapter (reader, writer, upload-URL
/// issuer, deleter) agrees on exactly the same mapping.
/// </summary>
internal static class BlobPathResolver
{
    /// <summary>Resolves a normalized relative object path to its container client and blob name.</summary>
    /// <param name="blobServiceClient">The Blob service client to resolve the container from.</param>
    /// <param name="normalizedRelativePath">The normalized relative path, e.g. <c>weekend/abc123.png</c>.</param>
    /// <returns>The container client and the blob name within it.</returns>
    /// <exception cref="ArgumentException">Thrown when the path has no folder segment to derive a container name from.</exception>
    public static (BlobContainerClient Container, string BlobName) Resolve(BlobServiceClient blobServiceClient, string normalizedRelativePath)
    {
        var trimmed = normalizedRelativePath.TrimStart('/');
        var separatorIndex = trimmed.IndexOf('/', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == trimmed.Length - 1)
        {
            throw new ArgumentException(
                $"Object path '{normalizedRelativePath}' has no folder prefix to resolve a container from.",
                nameof(normalizedRelativePath));
        }

        var containerName = trimmed[..separatorIndex];
        var blobName = trimmed[(separatorIndex + 1)..];
        return (blobServiceClient.GetBlobContainerClient(containerName), blobName);
    }
}
