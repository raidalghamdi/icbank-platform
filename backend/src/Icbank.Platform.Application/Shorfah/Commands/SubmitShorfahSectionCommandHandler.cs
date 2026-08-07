using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="SubmitShorfahSectionCommand"/>. Ports <c>shorfah.ts:400-418</c>: requires
/// <c>contribute</c> permission and non-empty content; no guard on current status (a section can
/// be submitted from any prior state, matching AMBIGUOUS-BR-1 verbatim).
/// </summary>
public sealed class SubmitShorfahSectionCommandHandler : IRequestHandler<SubmitShorfahSectionCommand, Result<ShorfahSectionDto>>
{
    /// <summary>The sentinel error returned when the caller lacks contribute permission.</summary>
    public const string ForbiddenError = "لست مساهماً في هذا القسم";

    /// <summary>The sentinel error returned when neither content field is populated.</summary>
    public const string EmptyContentError = "أضف المحتوى قبل التسليم";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SubmitShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SubmitShorfahSectionCommandHandler(
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
    public async Task<Result<ShorfahSectionDto>> Handle(SubmitShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        var allowed = await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Contribute, cancellationToken);
        if (!allowed)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        if (string.IsNullOrEmpty(section.ContentMd) && string.IsNullOrEmpty(section.ContentHtml))
        {
            return Result<ShorfahSectionDto>.Failure(EmptyContentError);
        }

        ShorfahWorkflowStatus fromStatus = section.WorkflowStatus;
        ApplySubmission(section, request);
        await PersistAndAuditAsync(section, request, fromStatus, cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }

    private void ApplySubmission(ShorfahSection section, SubmitShorfahSectionCommand request)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        section.WorkflowStatus = ShorfahWorkflowStatus.Submitted;
        section.ContributedByUserId = request.ActorUserId;
        section.ContributedAt = now;
        section.UpdatedAt = now.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
    }

    private async Task PersistAndAuditAsync(
        ShorfahSection section, SubmitShorfahSectionCommand request, ShorfahWorkflowStatus fromStatus, CancellationToken cancellationToken)
    {
        _dbContext.Add(new ShorfahWorkflowLog
        {
            SectionId = section.Id,
            ActorUserId = request.ActorUserId,
            Action = "submitted",
            FromStatus = fromStatus.ToString(),
            ToStatus = ShorfahWorkflowStatus.Submitted.ToString(),
            Notes = "تسليم المحتوى",
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.submit",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: new { status = fromStatus.ToString() },
            after: new { status = section.WorkflowStatus.ToString() },
            cancellationToken);
    }
}
