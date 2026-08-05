namespace Icbank.Platform.Application.Storage;

/// <summary>
/// Port for reading a stored media object by its normalized relative path. Implemented in
/// Infrastructure against whatever object-storage backend the deployment uses (the Node source
/// used Supabase Storage; this port keeps that swappable rather than hardwiring a provider into
/// Application, per R-BE-002).
/// </summary>
public interface IObjectStorageReader
{
    /// <summary>Attempts to open the object at the given normalized, already-validated relative path.</summary>
    /// <param name="normalizedRelativePath">The safe, normalized relative path (post <c>ISafeStoragePathValidator</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The object's content stream and content type, or <c>null</c> if it does not exist.</returns>
    Task<StoredObject?> OpenAsync(string normalizedRelativePath, CancellationToken cancellationToken);
}

/// <summary>A stream handle to a stored object.</summary>
/// <param name="Content">The object's raw byte content.</param>
/// <param name="ContentType">The object's MIME content type.</param>
public sealed record StoredObject(byte[] Content, string ContentType);
