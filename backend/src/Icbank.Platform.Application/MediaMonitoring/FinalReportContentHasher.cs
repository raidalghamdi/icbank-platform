using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Computes the <c>content_sha256</c> integrity fingerprint stored on every
/// <see cref="Domain.MediaMonitoring.FinalMediaReport"/> at creation time (BUSINESS-RULES.md
/// §5.2). This exists purely as an audit artifact -- nothing in the API re-verifies it against
/// current content, matching the Node source exactly.
/// </summary>
public static class FinalReportContentHasher
{
    /// <summary>Computes the lowercase hex SHA-256 hash of the given draft's JSON serialization.</summary>
    /// <param name="draft">The draft content to fingerprint.</param>
    /// <returns>The lowercase hex-encoded SHA-256 digest.</returns>
    public static string ComputeSha256(Commands.FinalReportDraftDto draft)
    {
        var json = JsonSerializer.Serialize(draft);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
