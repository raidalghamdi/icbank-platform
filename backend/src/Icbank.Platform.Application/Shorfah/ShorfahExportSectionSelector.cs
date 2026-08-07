using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// The section-selection rule shared verbatim across all three export endpoints (BUSINESS-RULES.md
/// §1.9): preview mode shows every <c>IncludeInPdf</c> section regardless of approval; final mode
/// shows only <c>approved</c> and <c>IncludeInPdf</c> sections. The Node source duplicated this
/// where-clause construction three times (<c>GET /pdf</c>, <c>GET /pdf.pdf</c>, <c>GET /docx</c>)
/// -- this port centralizes it in one place so the three export handlers cannot drift.
/// </summary>
public sealed class ShorfahExportSectionSelector
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ShorfahExportSectionSelector"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ShorfahExportSectionSelector(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <summary>Fetches the sections an export should render for the given issue and preview flag.</summary>
    /// <param name="issueId">The owning issue's id.</param>
    /// <param name="preview">
    /// When <c>true</c>, every <c>IncludeInPdf</c> section is returned regardless of workflow
    /// status. When <c>false</c>, only <c>approved</c> and <c>IncludeInPdf</c> sections are
    /// returned. This exact preview/final distinction must be preserved -- collapsing it would
    /// either leak unapproved content in "final" mode or hide legitimate drafts in "preview" mode.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected sections, ordered by display order.</returns>
    public async Task<List<ShorfahSection>> SelectAsync(int issueId, bool preview, CancellationToken cancellationToken)
    {
        IQueryable<ShorfahSection> query = _dbContext.ShorfahSections.Where(s => s.IssueId == issueId && s.IncludeInPdf);
        if (!preview)
        {
            query = query.Where(s => s.WorkflowStatus == ShorfahWorkflowStatus.Approved);
        }

        return await _queryExecutor.ToListAsync(query.OrderBy(s => s.DisplayOrder), cancellationToken);
    }
}
