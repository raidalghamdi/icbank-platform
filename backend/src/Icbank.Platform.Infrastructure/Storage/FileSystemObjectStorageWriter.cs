using Icbank.Platform.Application.Storage;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IObjectStorageWriter"/> implementation. Writes bytes to the local
/// filesystem storage root under a GUID-derived file name (never the client-influenced content),
/// closing the SEC-17 path-traversal class the same way <see cref="FileSystemObjectUploadUrlIssuer"/>
/// does for uploads. Swappable for a real cloud-blob-backed implementation without touching
/// Application.
/// </summary>
public sealed class FileSystemObjectStorageWriter : IObjectStorageWriter
{
    private readonly ObjectStorageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="FileSystemObjectStorageWriter"/> class.</summary>
    /// <param name="options">The bound storage-root configuration.</param>
    public FileSystemObjectStorageWriter(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(string folderPrefix, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var safeExtension = ContentTypeExtensions.Resolve(contentType);
        var relativePath = $"{folderPrefix.TrimEnd('/')}/{Guid.NewGuid():N}{safeExtension}";
        var absolutePath = Path.Combine(_options.RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(absolutePath, content, cancellationToken);
        return relativePath;
    }
}
