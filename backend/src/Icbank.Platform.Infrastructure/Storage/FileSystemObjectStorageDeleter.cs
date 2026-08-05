using Icbank.Platform.Application.Storage;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>
/// Default <see cref="IObjectStorageDeleter"/> implementation backed by the local filesystem root
/// (<see cref="ObjectStorageOptions.RootPath"/>), mirroring the other FileSystem* adapters in this
/// namespace so local development and the test suite need no cloud dependency.
/// </summary>
public sealed class FileSystemObjectStorageDeleter : IObjectStorageDeleter
{
    private readonly ObjectStorageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="FileSystemObjectStorageDeleter"/> class.</summary>
    /// <param name="options">The bound storage-root configuration.</param>
    public FileSystemObjectStorageDeleter(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string normalizedRelativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_options.RootPath, normalizedRelativePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }
}
