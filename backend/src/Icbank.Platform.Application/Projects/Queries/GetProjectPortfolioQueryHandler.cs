using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Projects;
using MediatR;

namespace Icbank.Platform.Application.Projects.Queries;

/// <summary>
/// Handles <see cref="GetProjectPortfolioQuery"/>. The page used to wait on an externally pushed
/// report and then recompute every badge in the browser; this returns the cards and the headline
/// figures already resolved in one round trip, so the portfolio paints as soon as the response
/// lands.
/// </summary>
public sealed class GetProjectPortfolioQueryHandler : IRequestHandler<GetProjectPortfolioQuery, Result<ProjectPortfolioDto>>
{
    private const int DueSoonWindowDays = 30;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="GetProjectPortfolioQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="clock">The clock port.</param>
    public GetProjectPortfolioQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<ProjectPortfolioDto>> Handle(GetProjectPortfolioQuery request, CancellationToken cancellationToken)
    {
        DateTime now = _clock.UtcNow.UtcDateTime;

        List<PortfolioProject> projects = await _queryExecutor.ToListAsync(
            _dbContext.PortfolioProjects
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.SortOrder)
                .ThenBy(p => p.Id),
            cancellationToken);

        List<ProjectMilestone> milestones = await _queryExecutor.ToListAsync(
            _dbContext.ProjectMilestones.OrderBy(m => m.ProjectId).ThenBy(m => m.SortOrder).ThenBy(m => m.Id),
            cancellationToken);

        List<ProjectProgressUpdate> progressUpdates = await _queryExecutor.ToListAsync(
            _dbContext.ProjectProgressUpdates.OrderByDescending(u => u.ReportedAt).ThenByDescending(u => u.Id),
            cancellationToken);

        ILookup<int, ProjectMilestone> byProject = milestones.ToLookup(m => m.ProjectId);
        ILookup<int, ProjectProgressUpdate> updatesByProject = progressUpdates.ToLookup(u => u.ProjectId);
        var cards = projects
            .Select(project => ProjectPortfolioMapper.ToDto(
                project,
                byProject[project.Id].ToList(),
                updatesByProject[project.Id].ToList(),
                now))
            .ToList();

        return Result<ProjectPortfolioDto>.Success(new ProjectPortfolioDto(BuildKpis(cards), cards, now));
    }

    private static ProjectPortfolioKpisDto BuildKpis(List<PortfolioProjectDto> cards)
    {
        var averageProgress = cards.Count == 0 ? 0 : (int)Math.Round(cards.Average(c => c.ProgressPercent));

        return new ProjectPortfolioKpisDto(
            cards.Count,
            cards.Count(c => string.Equals(c.Category, "operational", StringComparison.Ordinal)),
            cards.Count(c => string.Equals(c.Category, "strategic", StringComparison.Ordinal)),
            averageProgress,
            cards.Count(c => string.Equals(c.Health, "on_track", StringComparison.Ordinal)),
            cards.Count(c => string.Equals(c.Health, "at_risk", StringComparison.Ordinal)),
            cards.Count(c => string.Equals(c.Health, "delayed", StringComparison.Ordinal)),
            cards.Count(c => string.Equals(c.Health, "completed", StringComparison.Ordinal)),
            cards.Count(c => !string.Equals(c.Health, "completed", StringComparison.Ordinal) && c.DaysRemaining >= 0 && c.DaysRemaining <= DueSoonWindowDays));
    }
}
