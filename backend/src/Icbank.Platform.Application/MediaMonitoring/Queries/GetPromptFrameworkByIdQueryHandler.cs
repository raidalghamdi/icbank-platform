using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Handles <see cref="GetPromptFrameworkByIdQuery"/>.</summary>
public sealed class GetPromptFrameworkByIdQueryHandler : IRequestHandler<GetPromptFrameworkByIdQuery, Result<PromptFrameworkDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetPromptFrameworkByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetPromptFrameworkByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PromptFrameworkDto>> Handle(GetPromptFrameworkByIdQuery request, CancellationToken cancellationToken)
    {
        PromptFramework? framework = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.PromptFrameworks.Where(f => f.Id == request.FrameworkId), cancellationToken);
        return framework is null
            ? Result<PromptFrameworkDto>.Failure("القالب غير موجود")
            : Result<PromptFrameworkDto>.Success(PromptFrameworkMapper.ToDto(framework));
    }
}
