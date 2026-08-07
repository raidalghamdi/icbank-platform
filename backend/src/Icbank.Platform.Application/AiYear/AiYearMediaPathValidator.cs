using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.AiYear;

/// <summary>
/// Ports the Node source's <c>SAFE_OBJECT_PATH</c> regex (BUSINESS-RULES.md §3,
/// <c>ai-year.ts:31</c>): every media <c>objectPath</c> must literally be under the
/// <c>ai-year/2026/{month 1-12}/{activationId}/{filename}</c> structure the upload-URL endpoint
/// itself generates, preventing a client from pointing an activation at an arbitrary storage
/// path. This is a hard security/integrity rule, not a nicety.
/// </summary>
public static partial class AiYearMediaPathValidator
{
    /// <summary>Validates a single media object path against the required structure.</summary>
    /// <param name="objectPath">The candidate object path.</param>
    /// <returns><c>true</c> if the path matches the required structure.</returns>
    public static bool IsValid(string objectPath) => SafeObjectPathRegex().IsMatch(objectPath);

    [GeneratedRegex(@"^/objects/ai-year/2026/(1[0-2]|[1-9])/\d+/[\w.\-]+$")]
    private static partial Regex SafeObjectPathRegex();
}
