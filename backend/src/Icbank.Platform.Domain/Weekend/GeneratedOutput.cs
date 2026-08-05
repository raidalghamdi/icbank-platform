using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Weekend;

/// <summary>
/// AI-generated week-start message draft -- one row per model per generation
/// (DATA-MODEL.md section 3.9 <c>generated_outputs</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>archive_refs</c> was a <c>jsonb number[]</c> array of implied, unenforced
/// <see cref="ArchiveEntry"/> ids in the source schema. It remains a JSON-backed list here
/// rather than a join table, since it is a lightweight "used as inspiration" reference list with
/// no per-row metadata -- see DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class GeneratedOutput : AuditableEntity
{
    /// <summary>Gets or sets the message topic.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Gets or sets the generating model name: claude, openai, or gemini.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated output text.</summary>
    public string OutputText { get; set; } = string.Empty;

    /// <summary>Gets or sets the ids of the <see cref="ArchiveEntry"/> rows used as inspiration.</summary>
    public List<int> ArchiveRefIds { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether this is the human-approved draft.</summary>
    public bool Selected { get; set; }
}
