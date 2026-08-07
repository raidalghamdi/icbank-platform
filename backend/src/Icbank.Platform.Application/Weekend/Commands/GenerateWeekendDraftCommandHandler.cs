using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Handles <see cref="GenerateWeekendDraftCommand"/>. Ports BUSINESS-RULES.md §2.2's model-name
/// stamp (<c>gemini-2.5-flash</c>, not the stale DB column default) and creates the draft in
/// <c>pending_review</c> status.
/// </summary>
public sealed class GenerateWeekendDraftCommandHandler : IRequestHandler<GenerateWeekendDraftCommand, Result<WeekendDraftDto>>
{
    private const string GenerationModelName = "gemini-2.5-flash";
    private const string CityRiyadh = "الرياض";

    private readonly IApplicationDbContext _dbContext;
    private readonly IWeekendContentGenerator _contentGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="GenerateWeekendDraftCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="contentGenerator">The AI-backed (or placeholder) content generation port.</param>
    /// <param name="dateTimeProvider">The injectable clock, resolved to Asia/Riyadh for the default target date.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public GenerateWeekendDraftCommandHandler(
        IApplicationDbContext dbContext, IWeekendContentGenerator contentGenerator, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _contentGenerator = contentGenerator;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendDraftDto>> Handle(GenerateWeekendDraftCommand request, CancellationToken cancellationToken)
    {
        var weekendDate = string.IsNullOrWhiteSpace(request.WeekendDate)
            ? WeekendCadenceCalculator.NextThursday(_dateTimeProvider.RiyadhNow)
            : request.WeekendDate;

        var contentJson = await _contentGenerator.GenerateAsync(weekendDate, cancellationToken);

        var draft = new WeekendDraft
        {
            WeekendDate = weekendDate,
            City = CityRiyadh,
            Status = WeekendDraftStatus.PendingReview,
            ModelName = GenerationModelName,
            ContentJson = contentJson,
            GeneratedByUserId = request.ActorUserId,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        _dbContext.Add(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_draft.generate",
            "WeekendDraft",
            draft.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { draft.WeekendDate },
            cancellationToken);

        return Result<WeekendDraftDto>.Success(ToDto(draft));
    }

    private static WeekendDraftDto ToDto(WeekendDraft draft) => new(
        draft.Id,
        draft.WeekendDate,
        draft.City,
        draft.Status.ToString(),
        draft.ModelName,
        draft.ContentJson,
        draft.GeneratedByUserId,
        draft.ApprovedByUserId,
        draft.RejectedReason,
        draft.ApprovedAt,
        draft.PublishedAt);
}
