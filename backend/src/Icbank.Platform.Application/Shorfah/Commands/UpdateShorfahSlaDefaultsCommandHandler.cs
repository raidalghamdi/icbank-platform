using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="UpdateShorfahSlaDefaultsCommand"/>. Ports <c>shorfah.ts:276-316</c>:
/// <c>slaDays</c> clamped to [1, 60]; when propagating, only sections currently
/// <c>pending_contribution</c> or <c>rejected</c> for the matching type are retroactively updated
/// (BUSINESS-RULES.md §1.5 -- sections already submitted/in_review/approved are left untouched).
/// </summary>
public sealed class UpdateShorfahSlaDefaultsCommandHandler : IRequestHandler<UpdateShorfahSlaDefaultsCommand, Result<UpdateShorfahSlaDefaultsResultDto>>
{
    private const int MinSlaDays = 1;
    private const int MaxSlaDays = 60;
    private const int FallbackSlaDays = 7;

    private static readonly ShorfahWorkflowStatus[] PropagatableStatuses =
    {
        ShorfahWorkflowStatus.PendingContribution, ShorfahWorkflowStatus.Rejected,
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahSlaDefaultsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdateShorfahSlaDefaultsCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<UpdateShorfahSlaDefaultsResultDto>> Handle(UpdateShorfahSlaDefaultsCommand request, CancellationToken cancellationToken)
    {
        var shouldPropagate = request.Propagate != false;
        var propagatedSections = 0;
        DateTimeOffset now = _dateTimeProvider.UtcNow;

        foreach (ShorfahSlaDefaultEntry entry in request.Defaults)
        {
            if (!Enum.TryParse<ShorfahSectionType>(entry.SectionType, ignoreCase: true, out ShorfahSectionType sectionType))
            {
                continue;
            }

            var slaDays = Math.Clamp(entry.SlaDays == 0 ? FallbackSlaDays : entry.SlaDays, MinSlaDays, MaxSlaDays);
            await UpsertDefaultAsync(sectionType, slaDays, request.ActorUserId, now, cancellationToken);

            if (shouldPropagate)
            {
                propagatedSections += await PropagateToSectionsAsync(sectionType, slaDays, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_sla_defaults.update",
            "ShorfahSectionSlaDefault",
            "bulk",
            before: null,
            after: new { count = request.Defaults.Count, propagatedSections },
            cancellationToken);

        List<ShorfahSectionSlaDefault> rows = await _queryExecutor.ToListAsync(_dbContext.ShorfahSectionSlaDefaults, cancellationToken);
        var dtos = rows.Select(r => new ShorfahSlaDefaultDto(r.SectionType.ToString(), r.SlaDays)).ToList();

        return Result<UpdateShorfahSlaDefaultsResultDto>.Success(new UpdateShorfahSlaDefaultsResultDto(dtos, propagatedSections));
    }

    private async Task UpsertDefaultAsync(ShorfahSectionType sectionType, int slaDays, int actorUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ShorfahSectionSlaDefault? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSectionSlaDefaults.Where(d => d.SectionType == sectionType), cancellationToken);

        if (existing is not null)
        {
            existing.SlaDays = slaDays;
            existing.UpdatedAt = now.UtcDateTime;
            existing.UpdatedByUserId = actorUserId;
            return;
        }

        _dbContext.Add(new ShorfahSectionSlaDefault
        {
            SectionType = sectionType,
            SlaDays = slaDays,
            CreatedAt = now.UtcDateTime,
            CreatedBy = ShorfahMappers.IdString(actorUserId),
            UpdatedByUserId = actorUserId,
        });
    }

    private async Task<int> PropagateToSectionsAsync(ShorfahSectionType sectionType, int slaDays, CancellationToken cancellationToken)
    {
        List<ShorfahSection> targets = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahSections.Where(s => s.SectionType == sectionType && PropagatableStatuses.Contains(s.WorkflowStatus)),
            cancellationToken);

        foreach (ShorfahSection section in targets)
        {
            section.SlaDays = slaDays;
        }

        return targets.Count;
    }
}
