using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Brings an unpublished issue's paragraphs in line with
/// <see cref="ShorfahCanonicalSections.Templates"/>. <see cref="ShorfahSectionSeeder"/> only runs
/// when an issue is created, so every issue that already existed when the table of contents was
/// restructured would keep its old Arabic titles for ever and never gain the paragraphs added
/// since. This reconciler back-fills them on startup, in the same spirit as
/// <see cref="PortfolioProjectReconciler"/>.
/// </summary>
internal static class ShorfahCanonicalSectionReconciler
{
    /// <summary>The gap left between the display orders of dropped paragraphs parked after the canonical ones.</summary>
    internal const int DroppedOrderStep = 10;

    /// <summary>Builds the change set that brings one issue's paragraphs in line with the catalogue.</summary>
    /// <param name="issue">The issue, with <c>Sections</c> and each section's dependent collections loaded.</param>
    /// <param name="slaDefaultsByType">The configured SLA day counts, keyed by section type.</param>
    /// <returns>The reconciliation to apply, or <see cref="ShorfahSectionReconciliation.Empty"/> for a published issue.</returns>
    internal static ShorfahSectionReconciliation Reconcile(
        ShorfahIssue issue,
        IReadOnlyDictionary<ShorfahSectionType, int> slaDefaultsByType)
    {
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(slaDefaultsByType);

        // Why: a published issue is a historical artefact — its PDF is already out, so rewording
        // its paragraphs after the fact would falsify what was published.
        if (issue.Status == ShorfahIssueStatus.Published)
        {
            return ShorfahSectionReconciliation.Empty;
        }

        var sections = issue.Sections.OrderBy(section => section.Id).ToList();
        var inserted = new List<ShorfahSection>();
        var updated = new List<ShorfahSection>();

        foreach (ShorfahCanonicalSectionTemplate template in ShorfahCanonicalSections.Templates)
        {
            ShorfahSection? existing = sections.Find(section => section.SectionType == template.SectionType);
            if (existing is null)
            {
                inserted.Add(ShorfahSectionSeeder.BuildSection(
                    issue.Id,
                    template,
                    ShorfahSectionSeeder.SlaDaysFor(slaDefaultsByType, template.SectionType)));
                continue;
            }

            if (ApplyTemplate(existing, template))
            {
                updated.Add(existing);
            }
        }

        (List<ShorfahSection> removed, List<ShorfahSection> parked) = HandleDropped(sections);
        updated.AddRange(parked);
        return new ShorfahSectionReconciliation(inserted, updated, removed);
    }

    // Only the catalogue-owned presentation fields are overwritten: contributed content, workflow
    // state, SLA settings and every contributor/reviewer/approver field belong to the people using
    // the issue, not to the catalogue. Returns whether anything actually moved, so an
    // already-reconciled issue reports no change.
    private static bool ApplyTemplate(ShorfahSection section, ShorfahCanonicalSectionTemplate template)
    {
        var changed = !string.Equals(section.TitleAr, template.TitleAr, StringComparison.Ordinal)
            || !string.Equals(section.DescriptionAr, template.DescriptionAr, StringComparison.Ordinal)
            || section.DisplayOrder != template.DisplayOrder;

        section.TitleAr = template.TitleAr;
        section.DescriptionAr = template.DescriptionAr;
        section.DisplayOrder = template.DisplayOrder;
        return changed;
    }

    // A paragraph type the catalogue dropped is deleted only when nothing would be lost with it:
    // no content and no dependent rows. Anything an editor actually wrote survives and is parked
    // after the canonical paragraphs instead, so the issue still reads in the agreed order.
    private static (List<ShorfahSection> Removed, List<ShorfahSection> Parked) HandleDropped(List<ShorfahSection> sections)
    {
        var canonicalTypes = ShorfahCanonicalSections.Templates.Select(template => template.SectionType).ToHashSet();
        var dropped = sections.Where(section => !canonicalTypes.Contains(section.SectionType)).ToList();
        var removed = new List<ShorfahSection>();
        var parked = new List<ShorfahSection>();
        var parkedOrder = ParkingStartOrder();

        foreach (ShorfahSection section in dropped)
        {
            if (IsSafeToDelete(section))
            {
                removed.Add(section);
                continue;
            }

            if (section.DisplayOrder != parkedOrder)
            {
                section.DisplayOrder = parkedOrder;
                parked.Add(section);
            }

            parkedOrder += DroppedOrderStep;
        }

        return (removed, parked);
    }

    private static int ParkingStartOrder() =>
        ShorfahCanonicalSections.Templates.Max(template => template.DisplayOrder) + DroppedOrderStep;

    // Dependent rows are checked explicitly rather than trusted to a cascade: a reminder, workflow
    // log or media file attached to the paragraph is a record of real work, and deleting the
    // paragraph would take it with it.
    private static bool IsSafeToDelete(ShorfahSection section) =>
        string.IsNullOrWhiteSpace(section.ContentMd)
        && string.IsNullOrWhiteSpace(section.ContentHtml)
        && section.ChildSections.Count == 0
        && section.Permissions.Count == 0
        && section.Media.Count == 0
        && section.WorkflowLogs.Count == 0
        && section.Assignments.Count == 0
        && section.Reminders.Count == 0
        && section.Notifications.Count == 0;
}
