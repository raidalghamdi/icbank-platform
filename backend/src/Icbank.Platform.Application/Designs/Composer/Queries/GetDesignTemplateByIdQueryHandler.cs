using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Handles <see cref="GetDesignTemplateByIdQuery"/>.</summary>
public sealed class GetDesignTemplateByIdQueryHandler : IRequestHandler<GetDesignTemplateByIdQuery, Result<DesignTemplateDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetDesignTemplateByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetDesignTemplateByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<DesignTemplateDto>> Handle(GetDesignTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        DesignTemplate? entity = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.DesignTemplates.Where(t => t.Id == request.TemplateId), cancellationToken);
        return entity is null
            ? Result<DesignTemplateDto>.Failure("القالب غير موجود")
            : Result<DesignTemplateDto>.Success(DesignTemplateMapper.ToDto(entity));
    }
}
