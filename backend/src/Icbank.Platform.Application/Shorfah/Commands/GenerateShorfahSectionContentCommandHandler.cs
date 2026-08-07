using FluentValidation.Results;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Handles <see cref="GenerateShorfahSectionContentCommand"/>. Ports <c>shorfah.ts:471-513</c>:
/// AI section auto-generation. Admin-only, rate-limited (cost-abuse vector), and every AI JSON
/// response is validated (task requirement H-2 class) before it ever touches the row.
/// </summary>
public sealed class GenerateShorfahSectionContentCommandHandler : IRequestHandler<GenerateShorfahSectionContentCommand, Result<ShorfahSectionDto>>
{
    /// <summary>The sentinel error the controller maps to 429.</summary>
    public const string RateLimitedError = "تم تجاوز حد التوليد المؤقت، انتظر قليلاً وحاول مجدداً.";

    private static readonly ShorfahGeneratedSectionContentValidator ContentValidator = new();

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IShorfahSectionContentGenerator _generator;
    private readonly IShorfahSectionGenerationRateLimiter _rateLimiter;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="GenerateShorfahSectionContentCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="generator">The AI content generation port.</param>
    /// <param name="rateLimiter">The generation rate limiter.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public GenerateShorfahSectionContentCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IShorfahSectionContentGenerator generator,
        IShorfahSectionGenerationRateLimiter rateLimiter,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _generator = generator;
        _rateLimiter = rateLimiter;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ShorfahSectionDto>> Handle(GenerateShorfahSectionContentCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.ActorUserId))
        {
            return Result<ShorfahSectionDto>.Failure(RateLimitedError);
        }

        ShorfahSection? section = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.ShorfahSections.Where(s => s.Id == request.SectionId), cancellationToken);
        if (section is null)
        {
            return Result<ShorfahSectionDto>.Failure("القسم غير موجود");
        }

        var prompt = ShorfahGenerationPrompts.BuildPrompt(section.TitleAr, section.DescriptionAr, section.SectionType, section.GenerationPrompt);
        ShorfahGeneratedSectionContent generated = await _generator.GenerateAsync(prompt, cancellationToken);

        ValidationResult validation = ContentValidator.Validate(generated);
        if (!validation.IsValid)
        {
            return Result<ShorfahSectionDto>.Failure(validation.Errors[0].ErrorMessage);
        }

        ShorfahWorkflowStatus fromStatus = section.WorkflowStatus;
        ApplyGeneratedContent(section, generated, request);
        await PersistAndAuditAsync(section, generated, request, fromStatus, cancellationToken);

        return Result<ShorfahSectionDto>.Success(ShorfahMappers.ToDto(section));
    }

    private void ApplyGeneratedContent(ShorfahSection section, ShorfahGeneratedSectionContent generated, GenerateShorfahSectionContentCommand request)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        section.ContentMd = generated.ContentMd;
        section.WorkflowStatus = ShorfahWorkflowStatus.Submitted;
        section.ContributedByUserId = request.ActorUserId;
        section.ContributedAt = now;
        section.UpdatedAt = now.UtcDateTime;
        section.UpdatedBy = ShorfahMappers.IdString(request.ActorUserId);
    }

    private async Task PersistAndAuditAsync(
        ShorfahSection section,
        ShorfahGeneratedSectionContent generated,
        GenerateShorfahSectionContentCommand request,
        ShorfahWorkflowStatus fromStatus,
        CancellationToken cancellationToken)
    {
        _dbContext.Add(new ShorfahWorkflowLog
        {
            SectionId = section.Id,
            ActorUserId = request.ActorUserId,
            Action = "contributed",
            FromStatus = fromStatus.ToString(),
            ToStatus = ShorfahWorkflowStatus.Submitted.ToString(),
            Notes = "توليد آلي عبر الذكاء الاصطناعي",
            CreatedBy = ShorfahMappers.IdString(request.ActorUserId),
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "shorfah_section.generate",
            "ShorfahSection",
            ShorfahMappers.IdString(section.Id),
            before: null,
            after: new { length = generated.ContentMd.Length },
            cancellationToken);
    }
}
