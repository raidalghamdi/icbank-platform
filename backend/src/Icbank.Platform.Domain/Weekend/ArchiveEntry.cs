using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Weekend;

/// <summary>
/// Archive of past "week start" messages, used as a style reference and RAG context
/// (DATA-MODEL.md section 3.9 <c>archive_entries</c>, <c>week-start.ts</c> source file).
/// </summary>
/// <remarks>
/// Note: this table originates from the "Week Start" feature domain in the source schema
/// (<c>week-start.ts</c>), which has no dedicated folder in the mandated Domain layout. It is
/// grouped under <c>Weekend</c> as the closest related content-generation feature area -- see
/// DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class ArchiveEntry : AuditableEntity
{
    /// <summary>Gets or sets the archived message title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the archived message body.</summary>
    public string BodyText { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC date the message was originally used, if known.</summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>Gets or sets the occasion the message was written for.</summary>
    public string? Occasion { get; set; }

    /// <summary>Gets or sets the tone descriptor.</summary>
    public string? Tone { get; set; }

    /// <summary>Gets or sets the originating source file name, if imported.</summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// Gets or sets the embedding vector as a JSON float array. DATA-MODEL.md flags this as a
    /// brute-force, non-scaling approach with no SQL Server equivalent to pgvector; kept as-is
    /// for this port (no vector store migration performed) -- see DOMAIN-PORT-NOTES.md.
    /// </summary>
    public List<float>? Embedding { get; set; }
}
