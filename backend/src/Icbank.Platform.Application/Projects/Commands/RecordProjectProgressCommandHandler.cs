using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Projects;
using MediatR;

namespace Icbank.Platform.Application.Projects.Commands;

/// <summary>
/// Handles <see cref="RecordProjectProgressCommand"/>. The portfolio previously had no way to move
/// a project's percentage other than a database edit, and any such edit lost the reasoning behind
/// the number; this appends an auditable report and lets the card follow it.
/// </summary>
public sealed class RecordProjectProgressCommandHandler : IRequestHandler<RecordProjectProgressCommand, Result<PortfolioProjectDto>>
{
    private const int FullPercent = 100;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="RecordProjectProgressCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="clock">The clock port.</param>
    public RecordProjectProgressCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<PortfolioProjectDto>> Handle(RecordProjectProgressCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Result<PortfolioProjectDto>.Failure(validationError);
        }

        PortfolioProject? project = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.PortfolioProjects.Where(p => p.Id == request.ProjectId && p.IsActive),
            cancellationToken);
        if (project is null)
        {
            return Result<PortfolioProjectDto>.Failure(RecordProjectProgressCommand.ProjectNotFoundError);
        }

        DateTime now = _clock.UtcNow.UtcDateTime;
        ProjectProgressUpdate update = NewReport(request, project.Id, now);
        _dbContext.Add(update);

        project.ProgressPercent = request.ProgressPercent;
        project.LatestUpdate = update.Note;

        // Why: a manager reporting 100% is closing the project; leaving the stage at "in progress"
        // would keep it in the due-soon and at-risk counts for ever.
        if (request.ProgressPercent == FullPercent)
        {
            project.Stage = ProjectStage.Completed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<PortfolioProjectDto>.Success(await ProjectCardAsync(project, update, now, cancellationToken));
    }

    private static ProjectProgressUpdate NewReport(RecordProjectProgressCommand request, int projectId, DateTime now) =>
        new()
        {
            ProjectId = projectId,
            ProgressPercent = request.ProgressPercent,
            Note = request.Note.Trim(),
            ReportedBy = request.ReportedBy.Trim(),
            ReportedAt = now,
        };

    private static string? Validate(RecordProjectProgressCommand request)
    {
        if (request.ProgressPercent is < 0 or > FullPercent)
        {
            return RecordProjectProgressCommand.ProgressOutOfRangeError;
        }

        return string.IsNullOrWhiteSpace(request.Note) ? RecordProjectProgressCommand.EmptyNoteError : null;
    }

    private async Task<PortfolioProjectDto> ProjectCardAsync(
        PortfolioProject project,
        ProjectProgressUpdate update,
        DateTime now,
        CancellationToken cancellationToken)
    {
        List<ProjectMilestone> milestones = await _queryExecutor.ToListAsync(
            _dbContext.ProjectMilestones.Where(m => m.ProjectId == project.Id).OrderBy(m => m.SortOrder).ThenBy(m => m.Id),
            cancellationToken);
        List<ProjectProgressUpdate> history = await _queryExecutor.ToListAsync(
            _dbContext.ProjectProgressUpdates.Where(u => u.ProjectId == project.Id),
            cancellationToken);

        // Why: the appended report is authoritative right after the save, but a substituted
        // queryable in tests will not replay it, so it is unioned in explicitly.
        if (!history.Contains(update))
        {
            history.Add(update);
        }

        return ProjectPortfolioMapper.ToDto(project, milestones, history, now);
    }
}
