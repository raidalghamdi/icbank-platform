using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="MarkAllShorfahNotificationsReadCommand"/>. Ports <c>shorfah.ts:1023-1029</c>.</summary>
public sealed class MarkAllShorfahNotificationsReadCommandHandler : IRequestHandler<MarkAllShorfahNotificationsReadCommand, Result<int>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="MarkAllShorfahNotificationsReadCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public MarkAllShorfahNotificationsReadCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(MarkAllShorfahNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        List<ShorfahNotification> unread = await _queryExecutor.ToListAsync(
            _dbContext.ShorfahNotifications.Where(n => n.UserId == request.UserId && n.IsRead != true), cancellationToken);

        foreach (ShorfahNotification notification in unread)
        {
            notification.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(unread.Count);
    }
}
