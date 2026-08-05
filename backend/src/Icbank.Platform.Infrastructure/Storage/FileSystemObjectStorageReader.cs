using Icbank.Platform.Application.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IObjectStorageReader"/> implementation backed by a local filesystem root
/// (<see cref="ObjectStorageOptions.RootPath"/>). The Node source used Supabase Storage; this
/// port keeps the same normalized-relative-path contract so a cloud-blob-backed implementation
/// (Azure Blob Storage, S3) can be swapped in later without touching Application or the
/// controller — see WAVE1-PORT-NOTES.md for the explicit call-out that this is a placeholder
/// backend, not a claim that Supabase Storage itself was ported.
/// </summary>
public sealed class FileSystemObjectStorageReader : IObjectStorageReader
{
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private readonly ObjectStorageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="FileSystemObjectStorageReader"/> class.</summary>
    /// <param name="options">The bound storage-root configuration.</param>
    public FileSystemObjectStorageReader(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<StoredObject?> OpenAsync(string normalizedRelativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_options.RootPath, normalizedRelativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new StoredObject(bytes, contentType);
    }
}
