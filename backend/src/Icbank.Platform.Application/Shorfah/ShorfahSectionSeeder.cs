using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Seeds the 13 canonical sections for an issue (BUSINESS-RULES.md §1.2), pulling each section's
/// default SLA-day count from <c>shorfah_section_sla_defaults</c> (fallback 7 if no row exists),
/// matching <c>seedShorfahSections()</c>/<c>getSlaDefaultDays()</c> in <c>shorfah.ts:143-269</c>
/// exactly. Shared by <c>POST /shorfah/issues</c>, <c>POST /shorfah/issues/:id/seed-sections</c>,
/// and <c>POST /shorfah/issues/:id/collect</c> so the three call sites cannot drift.
/// </summary>
public sealed class ShorfahSectionSeeder
{
    private const int DefaultSlaDays = 7;

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

    /// <summary>Creates and tracks the 13 canonical sections for the given issue. Does not call <c>SaveChangesAsync</c> -- the caller controls the transaction boundary.</summary>
    /// <param name="issueId">The issue to seed sections for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sections seeded.</returns>
    public async Task<int> SeedAsync(int issueId, CancellationToken cancellationToken)
    {
        List<ShorfahSectionSlaDefault> defaults = await _queryExecutor.ToListAsync(_dbContext.ShorfahSectionSlaDefaults, cancellationToken);
        var defaultsByType = defaults.ToDictionary(d => d.SectionType, d => d.SlaDays);

        foreach (ShorfahCanonicalSectionTemplate template in ShorfahCanonicalSections.Templates)
        {
            var slaDays = defaultsByType.TryGetValue(template.SectionType, out var days) ? days : DefaultSlaDays;
            _dbContext.Add(new ShorfahSection
            {
                IssueId = issueId,
                SectionType = template.SectionType,
                TitleAr = template.TitleAr,
                DescriptionAr = template.DescriptionAr,
                DisplayOrder = template.DisplayOrder,
                IncludeInPdf = true,
                WorkflowStatus = ShorfahWorkflowStatus.PendingContribution,
                SlaDays = slaDays,
            });
        }

        return ShorfahCanonicalSections.Templates.Count;
    }
}
