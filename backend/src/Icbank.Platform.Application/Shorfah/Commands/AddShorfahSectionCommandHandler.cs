using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Handles <see cref="AddShorfahSectionCommand"/>. Ports <c>shorfah.ts:319-341</c>.</summary>
public sealed class AddShorfahSectionCommandHandler : IRequestHandler<AddShorfahSectionCommand, Result<ShorfahSectionDto>>
{
    private const int DefaultSlaDays = 7;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="AddShorfahSectionCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public AddShorfahSectionCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahSectionDto>> Handle(AddShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        var issueExists = await _queryExecutor.AnyAsync(_dbContext.ShorfahIssues.Where(i => i.Id == request.IssueId), cancellationToken);
        if (!issueExists)
        {
            return Result<ShorfahSectionDto>.Failure("العدد غير موجود");
        }

        ShorfahSection section = await BuildSectionAsync(request, cancellationToken);
        _dbContext.Add(section);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.create",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: null,
            after: new { section.IssueId, section.SectionType, section.TitleAr },
            cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }

    private async Task<ShorfahSection> BuildSectionAsync(AddShorfahSectionCommand request, CancellationToken cancellationToken)
    {
        ShorfahSectionType sectionType = Enum.Parse<ShorfahSectionType>(request.SectionType, ignoreCase: true);
        var slaDays = request.SlaDays ?? await DefaultSlaDaysForAsync(sectionType, cancellationToken);

        return new ShorfahSection
        {
            IssueId = request.IssueId,
            ParentSectionId = request.ParentSectionId,
            SectionType = sectionType,
            TitleAr = request.TitleAr,
            DescriptionAr = request.DescriptionAr,
            DisplayOrder = request.DisplayOrder ?? 0,
            OwnerUserId = request.OwnerUserId,
            OwnerRole = request.OwnerRole,
            AutoGenerate = request.AutoGenerate,
            GenerationPrompt = request.GenerationPrompt,
            WorkflowStatus = ShorfahWorkflowStatus.PendingContribution,
            IncludeInPdf = true,
            SlaDays = slaDays,
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        };
    }

    private async Task<int> DefaultSlaDaysForAsync(ShorfahSectionType sectionType, CancellationToken cancellationToken)
    {
        ShorfahSectionSlaDefault? row = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSectionSlaDefaults.Where(d => d.SectionType == sectionType), cancellationToken);
        return row?.SlaDays ?? DefaultSlaDays;
    }
}
