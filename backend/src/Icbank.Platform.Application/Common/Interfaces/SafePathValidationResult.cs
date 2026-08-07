namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>The outcome of a <see cref="ISafeStoragePathValidator"/> check.</summary>
/// <param name="IsValid">Whether <paramref name="NormalizedPath"/> is safe to use.</param>
/// <param name="NormalizedPath">The normalized, forward-slash path, only meaningful when <paramref name="IsValid"/> is <c>true</c>.</param>
/// <param name="RejectionReason">A machine-readable rejection reason, only meaningful when <paramref name="IsValid"/> is <c>false</c>.</param>
public sealed record SafePathValidationResult(bool IsValid, string? NormalizedPath, string? RejectionReason)
{
    /// <summary>Builds a successful result.</summary>
    /// <param name="normalizedPath">The normalized, safe path.</param>
    /// <returns>A valid <see cref="SafePathValidationResult"/>.</returns>
    public static SafePathValidationResult Valid(string normalizedPath) => new(true, normalizedPath, null);

    /// <summary>Builds a rejected result.</summary>
    /// <param name="reason">A machine-readable rejection reason, e.g. <c>traversal_segment</c>.</param>
    /// <returns>An invalid <see cref="SafePathValidationResult"/>.</returns>
    public static SafePathValidationResult Invalid(string reason) => new(false, null, reason);
}
