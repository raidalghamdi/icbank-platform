using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="PatchShorfahSectionCommand"/>. Ports <c>shorfah.ts:344-397</c>: field-level
/// RBAC gating (BUSINESS-RULES.md §1.4) -- content needs contribute-or-higher, <c>IncludeInPdf</c>
/// needs review-or-higher, everything else (title/order/description/SLA) is admin-only.
/// </summary>
public sealed class PatchShorfahSectionCommandHandler : IRequestHandler<PatchShorfahSectionCommand, Result<ShorfahSectionDto>>
{
    /// <summary>The sentinel error returned when the caller lacks any tier needed for the requested field(s).</summary>
    public const string ForbiddenError = "ليس لديك صلاحية لتحرير هذا الحقل";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionAccessService _accessService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="PatchShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="accessService">The per-section permission-tier port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public PatchShorfahSectionCommandHandler(
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
    public async Task<Result<ShorfahSectionDto>> Handle(PatchShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        Result<ShorfahSectionDto>? failure = await ApplyAllEditsAsync(request, section, cancellationToken);
        if (failure is { } editFailure)
        {
            return editFailure;
        }

        await PersistAndAuditAsync(request, section, cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }

    private static Result<ShorfahSectionDto>? ApplyAdminOnlyFields(PatchShorfahSectionCommand request, ShorfahSection section, bool isAdmin)
    {
        var touchesMetadata = request.TitleAr is not null || request.DisplayOrder.HasValue || request.DescriptionAr is not null;
        var touchesSla = request.SlaDays.HasValue || request.SlaStartsAt.HasValue || request.SlaDeadline.HasValue;
        if (!touchesMetadata && !touchesSla)
        {
            return null;
        }

        if (!isAdmin)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        ApplyMetadataFields(request, section);
        ApplySlaFields(request, section);

        return null;
    }

    private static void ApplyMetadataFields(PatchShorfahSectionCommand request, ShorfahSection section)
    {
        if (request.TitleAr is not null)
        {
            section.TitleAr = request.TitleAr;
        }

        if (request.DisplayOrder.HasValue)
        {
            section.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.DescriptionAr is not null)
        {
            section.DescriptionAr = request.DescriptionAr;
        }
    }

    private static void ApplySlaFields(PatchShorfahSectionCommand request, ShorfahSection section)
    {
        if (request.SlaDays.HasValue)
        {
            section.SlaDays = request.SlaDays;
        }

        if (request.SlaStartsAt.HasValue)
        {
            section.SlaStartsAt = request.SlaStartsAt;
        }

        if (request.SlaDeadline.HasValue)
        {
            section.SlaDeadline = request.SlaDeadline;
        }
    }

    private async Task<Result<ShorfahSectionDto>?> ApplyAllEditsAsync(PatchShorfahSectionCommand request, ShorfahSection section, CancellationToken cancellationToken)
    {
        var isAdmin = await _accessService.IsAdminAsync(request.ActorUserId, cancellationToken);

        Result<ShorfahSectionDto>? contentFailure = await ApplyContentEditsAsync(request, section, isAdmin, cancellationToken);
        if (contentFailure is { } failure)
        {
            return failure;
        }

        Result<ShorfahSectionDto>? includeFailure = await ApplyIncludeInPdfAsync(request, section, isAdmin, cancellationToken);
        if (includeFailure is { } includeFail)
        {
            return includeFail;
        }

        return ApplyAdminOnlyFields(request, section, isAdmin);
    }

    private async Task PersistAndAuditAsync(PatchShorfahSectionCommand request, ShorfahSection section, CancellationToken cancellationToken)
    {
        section.UpdatedAt = _dateTimeProvider.UtcNow.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.patch",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: null,
            after: new { section.TitleAr, section.IncludeInPdf, section.SlaDays },
            cancellationToken);
    }

    private async Task<Result<ShorfahSectionDto>?> ApplyContentEditsAsync(
        PatchShorfahSectionCommand request, ShorfahSection section, bool isAdmin, CancellationToken cancellationToken)
    {
        if (request.ContentMd is null && request.ContentHtml is null)
        {
            return null;
        }

        var canEditContent = isAdmin
            || await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Contribute, cancellationToken)
            || await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Review, cancellationToken)
            || await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Approve, cancellationToken);
        if (!canEditContent)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        if (request.ContentMd is not null)
        {
            section.ContentMd = request.ContentMd;
        }

        if (request.ContentHtml is not null)
        {
            section.ContentHtml = request.ContentHtml;
        }

        return null;
    }

    private async Task<Result<ShorfahSectionDto>?> ApplyIncludeInPdfAsync(
        PatchShorfahSectionCommand request, ShorfahSection section, bool isAdmin, CancellationToken cancellationToken)
    {
        if (request.IncludeInPdf is not { } includeInPdf)
        {
            return null;
        }

        var canToggle = isAdmin
            || await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Review, cancellationToken)
            || await _accessService.CanAccessSectionAsync(request.ActorUserId, section.Id, ShorfahSectionAccessTier.Approve, cancellationToken);
        if (!canToggle)
        {
            return Result<ShorfahSectionDto>.Failure(ForbiddenError);
        }

        section.IncludeInPdf = includeInPdf;
        _dbContext.Add(new ShorfahWorkflowLog
        {
            SectionId = section.Id,
            ActorUserId = request.ActorUserId,
            Action = "toggled_include",
            Notes = $"include_in_pdf set to {includeInPdf}",
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        return null;
    }
}
