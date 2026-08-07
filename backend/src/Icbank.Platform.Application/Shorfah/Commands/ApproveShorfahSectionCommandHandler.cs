using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="ApproveShorfahSectionCommand"/>. Ports <c>shorfah.ts:452-467</c>: requires
/// <c>approve</c> permission; no guard on current status at all -- approve can be called from any
/// state, including <c>pending_contribution</c> with no content (AMBIGUOUS-BR-1, preserved verbatim).
/// </summary>
public sealed class ApproveShorfahSectionCommandHandler : IRequestHandler<ApproveShorfahSectionCommand, Result<ShorfahSectionDto>>
{
    /// <summary>The sentinel error returned when the caller lacks approve permission.</summary>
    public const string ForbiddenError = "ليس لديك صلاحية اعتماد";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ApproveShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public ApproveShorfahSectionCommandHandler(
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
    public async Task<Result<ShorfahSectionDto>> Handle(ApproveShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        var allowed = await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Approve, cancellationToken);
        if (!allowed)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        ShorfahWorkflowStatus fromStatus = section.WorkflowStatus;
        ApplyApproval(section, request);
        await PersistAndAuditAsync(section, request, fromStatus, cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }

    private void ApplyApproval(ShorfahSection section, ApproveShorfahSectionCommand request)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        section.WorkflowStatus = ShorfahWorkflowStatus.Approved;
        section.ApprovedByUserId = request.ActorUserId;
        section.ApprovedAt = now;
        section.UpdatedAt = now.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
    }

    private async Task PersistAndAuditAsync(
        ShorfahSection section, ApproveShorfahSectionCommand request, ShorfahWorkflowStatus fromStatus, CancellationToken cancellationToken)
    {
        _dbContext.Add(new ShorfahWorkflowLog
        {
            SectionId = section.Id,
            ActorUserId = request.ActorUserId,
            Action = "approved",
            FromStatus = fromStatus.ToString(),
            ToStatus = ShorfahWorkflowStatus.Approved.ToString(),
            Notes = request.Notes,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.approve",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: new { status = fromStatus.ToString() },
            after: new { status = section.WorkflowStatus.ToString() },
            cancellationToken);
    }
}
