using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.InternationalDays;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>Handles <see cref="DeleteInternationalDayCommand"/>.</summary>
public sealed class DeleteInternationalDayCommandHandler : IRequestHandler<DeleteInternationalDayCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteInternationalDayCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteInternationalDayCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteInternationalDayCommand request, CancellationToken cancellationToken)
    {
        InternationalDay? day = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.InternationalDays.Where(d => d.Id == request.DayId), cancellationToken);
        if (day is null)
        {
            return Result<bool>.Failure("غير موجود");
        }

        _dbContext.Remove(day);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "international_day.delete",
            "InternationalDay",
            request.DayId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { day.DayNameAr },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
