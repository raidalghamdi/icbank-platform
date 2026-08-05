using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>
/// Handles <see cref="GenerateBackgroundsCommand"/>. Ports BUSINESS-RULES.md §7.3: generates 4
/// variants in parallel, tolerating partial failure (Node's <c>Promise.allSettled</c> semantics
/// -- this port awaits all 4 tasks via <see cref="Task.WhenAll{TResult}(System.Threading.Tasks.Task{TResult}[])"/>
/// wrapped so a single faulted task does not fail the others). Rate limited and audited per the
/// task's "image-generation endpoints are an external-cost abuse vector" instruction -- the Node
/// source had no rate limit on this route at all.
/// </summary>
public sealed class GenerateBackgroundsCommandHandler : IRequestHandler<GenerateBackgroundsCommand, Result<GenerateBackgroundsResultDto>>
{
    private const int VariantCount = 4;
    private const string StorageFolderPrefix = "designs/backgrounds/";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IBackgroundImageGenerator _imageGenerator;
    private readonly IObjectStorageWriter _storageWriter;
    private readonly IDesignGenerationRateLimiter _rateLimiter;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="GenerateBackgroundsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="imageGenerator">The AI-backed (or placeholder) image generation port.</param>
    /// <param name="storageWriter">The object-storage write port.</param>
    /// <param name="rateLimiter">The per-user generation rate limiter.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public GenerateBackgroundsCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IBackgroundImageGenerator imageGenerator,
        IObjectStorageWriter storageWriter,
        IDesignGenerationRateLimiter rateLimiter,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _imageGenerator = imageGenerator;
        _storageWriter = storageWriter;
        _rateLimiter = rateLimiter;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<GenerateBackgroundsResultDto>> Handle(GenerateBackgroundsCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.ActorUserId))
        {
            return Result<GenerateBackgroundsResultDto>.Failure("تم تجاوز حد التوليد المؤقت، انتظر دقيقة وحاول مجدداً.");
        }

        DesignTemplate? template = request.TemplateId is { } templateId
            ? await _queryExecutor.SingleOrDefaultAsync(_dbContext.DesignTemplates.Where(t => t.Id == templateId), cancellationToken)
            : null;
        var fullPrompt = BackgroundPromptBuilder.Build(request.Prompt, template);

        IEnumerable<Task<GeneratedBackgroundDto?>> tasks = Enumerable.Range(0, VariantCount).Select(_ => GenerateOneAsync(fullPrompt, cancellationToken));
        GeneratedBackgroundDto?[] outcomes = await Task.WhenAll(tasks);
        var succeeded = outcomes.Where(o => o is not null).Select(o => o!).ToList();

        if (succeeded.Count == 0)
        {
            return Result<GenerateBackgroundsResultDto>.Failure("فشل التوليد من مزوّد الصور");
        }

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.generate_backgrounds", "DesignTemplate", request.TemplateId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none", before: null, after: new { Count = succeeded.Count }, cancellationToken);

        return Result<GenerateBackgroundsResultDto>.Success(new GenerateBackgroundsResultDto(succeeded));
    }

    private async Task<GeneratedBackgroundDto?> GenerateOneAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            GeneratedBackgroundImage generated = await _imageGenerator.GenerateAsync(prompt, cancellationToken);
            var objectPath = await _storageWriter.SaveAsync(StorageFolderPrefix, generated.Content, generated.ContentType, cancellationToken);
            return new GeneratedBackgroundDto(objectPath, "gemini");
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
