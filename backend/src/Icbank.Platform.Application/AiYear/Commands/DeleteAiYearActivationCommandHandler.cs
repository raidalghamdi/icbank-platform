using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>
/// Handles <see cref="DeleteAiYearActivationCommand"/>. The Node source explicitly deleted
/// media/metrics before the activation in three separate statements despite a real DB CASCADE
/// already existing (BUSINESS-RULES.md §3: "harmless duplication, not a bug"); this port relies
/// on the real EF Core cascade-delete configured for these relationships instead of re-issuing
/// the redundant explicit deletes.
/// </summary>
public sealed class DeleteAiYearActivationCommandHandler : IRequestHandler<DeleteAiYearActivationCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteAiYearActivationCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteAiYearActivationCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteAiYearActivationCommand request, CancellationToken cancellationToken)
    {
        AiYearActivation? activation = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == request.ActivationId), cancellationToken);
        if (activation is null)
        {
            return Result<bool>.Failure("التفعيل غير موجود");
        }

        _dbContext.Remove(activation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "ai_year_activation.delete",
            "AiYearActivation",
            request.ActivationId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { activation.Title },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
