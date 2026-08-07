using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="MarkShorfahNotificationReadCommand"/>. Ports <c>shorfah.ts:1011-1021</c>.</summary>
public sealed class MarkShorfahNotificationReadCommandHandler : IRequestHandler<MarkShorfahNotificationReadCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="MarkShorfahNotificationReadCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public MarkShorfahNotificationReadCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(MarkShorfahNotificationReadCommand request, CancellationToken cancellationToken)
    {
        ShorfahNotification? notification = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahNotifications.Where(n => n.Id == request.NotificationId && n.UserId == request.UserId), cancellationToken);
        if (notification is null)
        {
            return Result<bool>.Failure("الإشعار غير موجود");
        }

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
