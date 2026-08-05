using System.Text;
using Icbank.Platform.Application.Common.Interfaces;

namespace Icbank.Platform.Infrastructure.Security;

/// <summary>
/// Default <see cref="ISafeStoragePathValidator"/> implementation (closes SEC-17). Unlike the old
/// system's bare <c>startsWith()</c> allowlist check (BUSINESS-RULES.md §12.2), this validator:
/// (1) rejects null/control bytes outright, (2) URL-decodes up to a fixed number of passes so an
/// encoded traversal segment (<c>%2e%2e%2f</c>, double-encoded, mixed-case hex) cannot smuggle a
/// literal <c>..</c> past a naive single-decode check, (3) rejects absolute paths and UNC/drive
/// prefixes on both Windows and POSIX conventions, (4) normalizes with
/// <see cref="Path.GetFullPath(string)"/> against a fixed virtual root and re-verifies the result
/// still lives under that root (the actual defense against traversal — string matching on the
/// input is inherently bypassable, but comparing the fully resolved path against the root is
/// not), and only then (5) checks the caller's prefix allowlist against the normalized path.
/// </summary>
public sealed class SafeStoragePathValidator : ISafeStoragePathValidator
{
    // Why: a fixed, non-existent virtual root purely to anchor Path.GetFullPath's normalization —
    // this validator never touches the real filesystem, it only needs .NET's own path-resolution
    // algorithm to collapse ".." segments so the result can be compared against the root.
    private const string VirtualRoot = "/__icbank_storage_root__";
    private const int MaxDecodePasses = 5;

    /// <inheritdoc />
    public SafePathValidationResult Validate(string candidatePath, IReadOnlyCollection<string> allowedPrefixes)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return SafePathValidationResult.Invalid("empty_path");
        }

        if (ContainsNullOrControlBytes(candidatePath))
        {
            return SafePathValidationResult.Invalid("null_or_control_byte");
        }

        var decoded = DecodeRepeatedly(candidatePath);
        if (decoded is null)
        {
            return SafePathValidationResult.Invalid("decode_limit_exceeded");
        }

        SafePathValidationResult? decodedRejection = RejectDecodedForm(candidatePath, decoded);
        if (decodedRejection is not null)
        {
            return decodedRejection;
        }

        var normalizedSlashes = decoded.Replace('\\', '/');
        return ResolveAgainstRoot(normalizedSlashes, allowedPrefixes);
    }

    private static bool ContainsNullOrControlBytes(string value)
    {
        return value.Any(character => character == '\0' || (char.IsControl(character) && character != '\t'));
    }

    private static string? DecodeRepeatedly(string value)
    {
        var current = value;
        for (var pass = 0; pass < MaxDecodePasses; pass++)
        {
            var next = Uri.UnescapeDataString(current);
            if (string.Equals(next, current, StringComparison.Ordinal))
            {
                return current;
            }

            current = next;
        }

        // Why: a path requiring more than MaxDecodePasses rounds of decoding to stabilize is
        // treated as hostile input rather than risked being under-decoded and passed through.
        return null;
    }

    /// <summary>
    /// Checks the post-decode invariants that must hold regardless of how the input was encoded:
    /// no smuggled control bytes and no UNC prefix on either the raw or decoded form.
    /// </summary>
    private static SafePathValidationResult? RejectDecodedForm(string candidatePath, string decoded)
    {
        if (ContainsNullOrControlBytes(decoded))
        {
            return SafePathValidationResult.Invalid("encoded_null_or_control_byte");
        }

        if (IsUncPath(candidatePath) || IsUncPath(decoded))
        {
            return SafePathValidationResult.Invalid("unc_path");
        }

        return null;
    }

    private static bool IsAbsoluteOrRooted(string normalizedSlashes)
    {
        if (normalizedSlashes.StartsWith('/'))
        {
            return true;
        }

        // Windows drive-letter absolute path, e.g. "C:/Windows" or "c:\\Windows".
        if (normalizedSlashes.Length >= 2 && char.IsLetter(normalizedSlashes[0]) && normalizedSlashes[1] == ':')
        {
            return true;
        }

        return Path.IsPathRooted(normalizedSlashes);
    }

    private static bool IsUncPath(string value)
    {
        return value.StartsWith("\\\\", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal);
    }

    /// <summary>
    /// Normalizes <paramref name="normalizedSlashes"/> against <see cref="VirtualRoot"/>, verifies
    /// the resolved path did not escape the root, and checks the caller's prefix allowlist.
    /// </summary>
    private static SafePathValidationResult ResolveAgainstRoot(
        string normalizedSlashes, IReadOnlyCollection<string> allowedPrefixes)
    {
        if (IsAbsoluteOrRooted(normalizedSlashes))
        {
            return SafePathValidationResult.Invalid("absolute_or_rooted_path");
        }

        var resolvedSlashes = TryResolveFullPath(normalizedSlashes, out SafePathValidationResult? failure);
        if (failure is not null)
        {
            return failure;
        }

        if (!StaysWithinVirtualRoot(resolvedSlashes!))
        {
            // Why: this is the actual traversal defense — if resolving ".." segments walked the
            // candidate path outside the virtual root, no amount of string-matching upstream
            // would have caught every encoding of the escape.
            return SafePathValidationResult.Invalid("traversal_escapes_root");
        }

        var normalizedRelative = resolvedSlashes![(VirtualRoot.Length + 1)..].TrimStart('/');
        return CheckAllowedPrefix(normalizedRelative, allowedPrefixes);
    }

    /// <summary>Resolves <paramref name="normalizedSlashes"/> under <see cref="VirtualRoot"/> via .NET's own path-resolution algorithm.</summary>
    private static string? TryResolveFullPath(string normalizedSlashes, out SafePathValidationResult? failure)
    {
        try
        {
            failure = null;
            return Path.GetFullPath(VirtualRoot + "/" + normalizedSlashes).Replace('\\', '/');
        }
        catch (ArgumentException)
        {
            failure = SafePathValidationResult.Invalid("unresolvable_path");
            return null;
        }
    }

    private static bool StaysWithinVirtualRoot(string resolvedSlashes)
    {
        return resolvedSlashes.StartsWith(VirtualRoot + "/", StringComparison.Ordinal) || resolvedSlashes == VirtualRoot;
    }

    private static SafePathValidationResult CheckAllowedPrefix(
        string normalizedRelative, IReadOnlyCollection<string> allowedPrefixes)
    {
        if (allowedPrefixes.Count > 0 &&
            !allowedPrefixes.Any(prefix => normalizedRelative.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return SafePathValidationResult.Invalid("prefix_not_allowed");
        }

        return SafePathValidationResult.Valid(normalizedRelative);
    }
}
