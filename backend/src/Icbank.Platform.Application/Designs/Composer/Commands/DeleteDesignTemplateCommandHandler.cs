using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Handles <see cref="DeleteDesignTemplateCommand"/>. Hard delete, matching the Node source (lookup-table-style reference data, no soft-delete concept in the original schema).</summary>
public sealed class DeleteDesignTemplateCommandHandler : IRequestHandler<DeleteDesignTemplateCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteDesignTemplateCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public DeleteDesignTemplateCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteDesignTemplateCommand request, CancellationToken cancellationToken)
    {
        DesignTemplate? entity = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.DesignTemplates.Where(t => t.Id == request.TemplateId), cancellationToken);
        if (entity is null)
        {
            return Result<bool>.Failure("القالب غير موجود");
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.template.delete", "DesignTemplate", request.TemplateId.ToString(System.Globalization.CultureInfo.InvariantCulture), before: new { entity.TemplateNameAr }, after: null, cancellationToken);

        return Result<bool>.Success(true);
    }
}
