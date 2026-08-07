using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="ReviewShorfahSectionCommand"/>. Ports <c>shorfah.ts:420-441</c>: requires
/// <c>review</c> permission; no guard requiring the section be in <c>submitted</c> state first
/// (AMBIGUOUS-BR-1, preserved verbatim).
/// </summary>
public sealed class ReviewShorfahSectionCommandHandler : IRequestHandler<ReviewShorfahSectionCommand, Result<ShorfahSectionDto>>
{
    /// <summary>The sentinel error returned when the caller lacks review permission.</summary>
    public const string ForbiddenError = "ليس لديك صلاحية مراجعة";

    private const string RejectDecision = "reject";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ReviewShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public ReviewShorfahSectionCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IShorfahSectionAccessService accessService,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _accessService = accessService;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahSectionDto>> Handle(ReviewShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        var allowed = await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Review, cancellationToken);
        if (!allowed)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        ShorfahWorkflowStatus fromStatus = section.WorkflowStatus;
        var isReject = ApplyDecision(section, request);
        var action = isReject ? "rejected" : "reviewed";
        return await PersistTransitionAsync(request, section, fromStatus, action, cancellationToken);
    }

    /// <summary>Applies the reviewer's decision to the section and returns whether it was a rejection.</summary>
    private bool ApplyDecision(ShorfahSection section, ReviewShorfahSectionCommand request)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        var isReject = string.Equals(request.Decision, RejectDecision, StringComparison.OrdinalIgnoreCase);

        section.WorkflowStatus = isReject ? ShorfahWorkflowStatus.Rejected : ShorfahWorkflowStatus.InReview;
        section.ReviewedByUserId = request.ActorUserId;
        section.ReviewedAt = now;
        section.UpdatedAt = now.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);

        if (isReject)
        {
            section.RejectionReason = request.Notes;
            return true;
        }

        section.ReviewNotes = request.Notes;
        return false;
    }

    private async Task<Result<ShorfahSectionDto>> PersistTransitionAsync(
        ReviewShorfahSectionCommand request, ShorfahSection section, ShorfahWorkflowStatus fromStatus, string action, CancellationToken cancellationToken)
    {
        _dbContext.Add(new ShorfahWorkflowLog
        {
            SectionId = section.Id,
            ActorUserId = request.ActorUserId,
            Action = action,
            FromStatus = fromStatus.ToString(),
            ToStatus = section.WorkflowStatus.ToString(),
            Notes = request.Notes,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.review",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: new { status = fromStatus.ToString() },
            after: new { status = section.WorkflowStatus.ToString() },
            cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }
}
