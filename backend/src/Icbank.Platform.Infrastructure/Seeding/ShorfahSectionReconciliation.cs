using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The change set <see cref="ShorfahCanonicalSectionReconciler"/> produced for one issue's
/// paragraphs. Returned as data rather than applied inline so the reconciliation rules can be
/// exercised without a database.
/// </summary>
/// <param name="Inserted">Canonical paragraphs the issue was missing.</param>
/// <param name="Updated">Existing paragraphs whose title, definition or position was refreshed.</param>
/// <param name="Removed">Dropped paragraphs that were empty and safe to delete.</param>
internal sealed record ShorfahSectionReconciliation(
    IReadOnlyList<ShorfahSection> Inserted,
    IReadOnlyList<ShorfahSection> Updated,
    IReadOnlyList<ShorfahSection> Removed)
{
    /// <summary>Gets an empty change set, used for issues the reconciler must not touch.</summary>
    internal static ShorfahSectionReconciliation Empty { get; } = new(
        Array.Empty<ShorfahSection>(),
        Array.Empty<ShorfahSection>(),
        Array.Empty<ShorfahSection>());

    /// <summary>Gets a value indicating whether the reconciliation would change anything at all.</summary>
    internal bool HasChanges => Inserted.Count > 0 || Updated.Count > 0 || Removed.Count > 0;
}
