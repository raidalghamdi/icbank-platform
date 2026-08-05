namespace Icbank.Platform.Application.Storage;

/// <summary>
/// Port for deleting a previously stored object by its normalized relative path. Added to close
/// WAVE1-PORT-NOTES.md item 23: deleting a <c>WeekendPlace</c> (or any other entity that owns an
/// uploaded object) must also remove the underlying blob, rather than orphaning it the way the
/// filesystem-only Wave 1 port did before a storage-delete capability existed.
/// </summary>
public interface IObjectStorageDeleter
{
    /// <summary>Deletes the object at the given normalized, already-validated relative path, if it exists.</summary>
    /// <param name="normalizedRelativePath">The safe, normalized relative path (post <c>ISafeStoragePathValidator</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> if an object was found and deleted; <see langword="false"/> if no
    /// object existed at that path (deleting something already gone is not an error — the caller's
    /// goal, "this object must not exist afterward", is already satisfied).
    /// </returns>
    Task<bool> DeleteAsync(string normalizedRelativePath, CancellationToken cancellationToken);
}
