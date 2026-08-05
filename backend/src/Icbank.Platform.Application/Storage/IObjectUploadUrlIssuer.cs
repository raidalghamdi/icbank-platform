namespace Icbank.Platform.Application.Storage;

/// <summary>
/// Port for issuing a short-lived presigned upload URL (BUSINESS-RULES.md §12.3's two-step
/// pattern: client requests a URL, PUTs bytes directly to storage, then calls a create/update
/// endpoint referencing the returned <see cref="PresignedUpload.ObjectPath"/>).
/// </summary>
public interface IObjectUploadUrlIssuer
{
    /// <summary>Issues a presigned upload URL under the given folder prefix.</summary>
    /// <param name="folderPrefix">The storage folder prefix the object will be namespaced under (e.g. <c>weekend/</c>).</param>
    /// <param name="fileName">The client-supplied original file name, used to derive a safe extension.</param>
    /// <param name="contentType">The optional MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The presigned upload descriptor.</returns>
    Task<PresignedUpload> IssueAsync(string folderPrefix, string fileName, string? contentType, CancellationToken cancellationToken);
}

/// <summary>A presigned upload descriptor.</summary>
/// <param name="UploadUrl">The short-lived URL the client PUTs bytes to.</param>
/// <param name="ObjectPath">The canonical object path the client must record and reference afterward.</param>
public sealed record PresignedUpload(string UploadUrl, string ObjectPath);
