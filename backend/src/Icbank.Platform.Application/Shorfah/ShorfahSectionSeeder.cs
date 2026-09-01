using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Seeds the 18 canonical paragraphs for an issue (BUSINESS-RULES.md §1.2), pulling each section's
/// default SLA-day count from <c>shorfah_section_sla_defaults</c> (fallback 7 if no row exists),
/// matching <c>seedShorfahSections()</c>/<c>getSlaDefaultDays()</c> in <c>shorfah.ts:143-269</c>
/// exactly. Shared by <c>POST /shorfah/issues</c>, <c>POST /shorfah/issues/:id/seed-sections</c>,
/// and <c>POST /shorfah/issues/:id/collect</c> so the three call sites cannot drift.
/// </summary>
public sealed class ShorfahSectionSeeder
{
    /// <summary>The SLA day count used when <c>shorfah_section_sla_defaults</c> has no row for a section type.</summary>
    public const int DefaultSlaDays = 7;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ShorfahSectionSeeder"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ShorfahSectionSeeder(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <summary>Creates one canonical paragraph in its initial, un-contributed state.</summary>
    /// <param name="issueId">The owning issue.</param>
    /// <param name="template">The canonical template the paragraph is built from.</param>
    /// <param name="slaDays">The SLA day count for the paragraph.</param>
    /// <returns>The untracked section.</returns>
    /// <remarks>Shared with the startup reconciler that back-fills paragraphs onto issues created before a catalogue change, so a newly inserted paragraph is identical however it arrives.</remarks>
    public static ShorfahSection BuildSection(int issueId, ShorfahCanonicalSectionTemplate template, int slaDays)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new ShorfahSection
        {
            IssueId = issueId,
            SectionType = template.SectionType,
            TitleAr = template.TitleAr,
            DescriptionAr = template.DescriptionAr,
            DisplayOrder = template.DisplayOrder,
            IncludeInPdf = true,
            WorkflowStatus = ShorfahWorkflowStatus.PendingContribution,
            SlaDays = slaDays,
        };
    }

    /// <summary>Resolves a section type's SLA day count, falling back to <see cref="DefaultSlaDays"/>.</summary>
    /// <param name="defaultsByType">The configured defaults, keyed by section type.</param>
    /// <param name="sectionType">The section type to resolve.</param>
    /// <returns>The SLA day count.</returns>
    public static int SlaDaysFor(IReadOnlyDictionary<ShorfahSectionType, int> defaultsByType, ShorfahSectionType sectionType)
    {
        ArgumentNullException.ThrowIfNull(defaultsByType);

        return defaultsByType.TryGetValue(sectionType, out var days) ? days : DefaultSlaDays;
    }

    /// <summary>Creates and tracks the 18 canonical paragraphs for the given issue. Does not call <c>SaveChangesAsync</c> -- the caller controls the transaction boundary.</summary>
    /// <param name="issueId">The issue to seed sections for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sections seeded.</returns>
    public async Task<int> SeedAsync(int issueId, CancellationToken cancellationToken)
    {
        List<ShorfahSectionSlaDefault> defaults = await _queryExecutor.ToListAsync(_dbContext.ShorfahSectionSlaDefaults, cancellationToken);
        var defaultsByType = defaults.ToDictionary(d => d.SectionType, d => d.SlaDays);

        foreach (ShorfahCanonicalSectionTemplate template in ShorfahCanonicalSections.Templates)
        {
            _dbContext.Add(BuildSection(issueId, template, SlaDaysFor(defaultsByType, template.SectionType)));
        }

        return ShorfahCanonicalSections.Templates.Count;
    }
}
