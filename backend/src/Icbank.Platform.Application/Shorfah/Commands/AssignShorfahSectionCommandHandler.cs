using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="AssignShorfahSectionCommand"/>. Ports <c>shorfah.ts:871-882</c>.</summary>
public sealed class AssignShorfahSectionCommandHandler : IRequestHandler<AssignShorfahSectionCommand, Result<ShorfahAssignmentDto>>
{
    private const string DefaultRole = "contributor";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="AssignShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public AssignShorfahSectionCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahAssignmentDto>> Handle(AssignShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        var sectionExists = await _queryExecutor.AnyAsync(_dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (!sectionExists)
        {
            return Result<ShorfahAssignmentDto>.Failure("القسم غير موجود");
        }

        var userExists = await _queryExecutor.AnyAsync(_dbContext.Users.Where(u => u.Id == request.UserId), cancellationToken);
        if (!userExists)
        {
            return Result<ShorfahAssignmentDto>.Failure("المستخدم غير موجود");
        }

        var assignment = new ShorfahAssignment
        {
            SectionId = request.SectionId,
            UserId = request.UserId,
            Role = string.IsNullOrWhiteSpace(request.Role) ? DefaultRole : request.Role,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        };
        _dbContext.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_assignment.create",
            "ShorfahAssignment",
            ShorfahMappers.IdString(assignment.Id),
            before: null,
            after: new { assignment.SectionId, assignment.UserId, assignment.Role },
            cancellationToken);

        return Result<ShorfahAssignmentDto>.Success(ShorfahMappers.ToDto(assignment));
    }
}
