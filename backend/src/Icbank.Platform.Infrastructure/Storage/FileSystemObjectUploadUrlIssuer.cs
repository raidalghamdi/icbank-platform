using Icbank.Platform.Application.Storage;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IObjectUploadUrlIssuer"/> implementation. The Node source issued a
/// short-lived (900-second) Supabase Storage presigned PUT URL; this placeholder implementation
/// issues a self-referential API URL under the configured storage root instead of a real
/// cloud-blob presigned URL, since no cloud storage provider is wired up in this port (see
/// WAVE1-PORT-NOTES.md — the object path/contract is faithfully ported, the presigning backend is
/// deferred). Swappable for a real Azure Blob/S3-backed implementation without touching
/// Application.
/// </summary>
public sealed class FileSystemObjectUploadUrlIssuer : IObjectUploadUrlIssuer
{
    private readonly ObjectStorageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="FileSystemObjectUploadUrlIssuer"/> class.</summary>
    /// <param name="options">The bound storage-root configuration.</param>
    public FileSystemObjectUploadUrlIssuer(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<PresignedUpload> IssueAsync(string folderPrefix, string fileName, string? contentType, CancellationToken cancellationToken)
    {
        var safeExtension = Path.GetExtension(fileName);
        var objectPath = $"{folderPrefix.TrimEnd('/')}/{Guid.NewGuid():N}{safeExtension}";
        var uploadUrl = $"{_options.UploadUrlBase.TrimEnd('/')}/{objectPath}";
        return Task.FromResult(new PresignedUpload(uploadUrl, objectPath));
    }
}
