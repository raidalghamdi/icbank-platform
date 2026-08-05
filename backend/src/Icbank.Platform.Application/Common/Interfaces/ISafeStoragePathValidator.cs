namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for hardened storage-path/filename validation (closes SEC-17: "Path-traversal
/// defense-in-depth gap in <c>/storage/objects/*</c>... <c>startsWith()</c> allowlist check
/// doesn't reject <c>..</c> segments or normalize the path first"). Any code that accepts a
/// client-supplied relative path or filename destined for object storage or the filesystem must
/// call this before using the value, even if a prefix allowlist is also applied — normalization
/// happens first so the allowlist check that follows cannot be bypassed by an unnormalized
/// traversal segment.
/// </summary>
public interface ISafeStoragePathValidator
{
    /// <summary>
    /// Validates and normalizes a client-supplied relative storage path.
    /// </summary>
    /// <param name="candidatePath">The untrusted, client-supplied relative path.</param>
    /// <param name="allowedPrefixes">
    /// The set of allowed leading segments (e.g. <c>shorfah/</c>, <c>gac/</c>) the normalized
    /// path must start with. Pass an empty collection to skip the prefix check (validation of
    /// traversal/absolute/null-byte/UNC safety still always applies).
    /// </param>
    /// <returns>
    /// A <see cref="SafePathValidationResult"/> carrying the normalized, safe path on success, or
    /// the specific rejection reason on failure.
    /// </returns>
    SafePathValidationResult Validate(string candidatePath, IReadOnlyCollection<string> allowedPrefixes);
}
