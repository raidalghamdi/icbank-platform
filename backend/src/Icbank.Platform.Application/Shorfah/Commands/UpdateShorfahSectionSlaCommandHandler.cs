using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="UpdateShorfahSectionSlaCommand"/>. Ports <c>shorfah.ts:854-869</c>: when
/// <c>SlaStartsAt</c> is supplied, the deadline is recomputed as <c>SlaStartsAt + SlaDays</c> in
/// calendar days (BUSINESS-RULES.md §1.6) -- date arithmetic is anchored to Asia/Riyadh via
/// <see cref="IDateTimeProvider"/> rather than server-local time.
/// </summary>
public sealed class UpdateShorfahSectionSlaCommandHandler : IRequestHandler<UpdateShorfahSectionSlaCommand, Result<ShorfahSectionDto>>
{
    private const int DefaultSlaDays = 7;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahSectionSlaCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdateShorfahSectionSlaCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahSectionDto>> Handle(UpdateShorfahSectionSlaCommand request, CancellationToken cancellationToken)
    {
        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        if (request.SlaDays.HasValue)
        {
            section.SlaDays = request.SlaDays;
        }

        if (request.SlaStartsAt.HasValue)
        {
            var slaDays = request.SlaDays ?? section.SlaDays ?? DefaultSlaDays;
            section.SlaStartsAt = request.SlaStartsAt;
            section.SlaDeadline = request.SlaStartsAt.Value.AddDays(slaDays);
        }

        section.UpdatedAt = _dateTimeProvider.UtcNow.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.sla_update",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: null,
            after: new { section.SlaDays, section.SlaStartsAt, section.SlaDeadline },
            cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }
}
