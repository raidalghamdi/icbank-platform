using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="UploadArchiveDocumentsCommand"/>.</summary>
public sealed class UploadArchiveDocumentsCommandHandler
    : IRequestHandler<UploadArchiveDocumentsCommand, Result<UploadArchiveDocumentsResultDto>>
{
    private const string NoExtractableTextReason = "لا يوجد نص قابل للاستخراج";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDocumentTextExtractor _textExtractor;

    /// <summary>Initializes a new instance of the <see cref="UploadArchiveDocumentsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="textExtractor">The document text-extraction port.</param>
    public UploadArchiveDocumentsCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDocumentTextExtractor textExtractor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _textExtractor = textExtractor;
    }

    /// <inheritdoc />
    public async Task<Result<UploadArchiveDocumentsResultDto>> Handle(UploadArchiveDocumentsCommand request, CancellationToken cancellationToken)
    {
        var results = new List<UploadedDocumentResultDto>();
        foreach (UploadedDocument file in request.Files)
        {
            results.Add(await ProcessFileAsync(file, request.ActorUserId, cancellationToken));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RecomputeStyleProfileAsync(request.ActorUserId, cancellationToken);

        var processedCount = results.Count(r => r.Id.HasValue);
        return Result<UploadArchiveDocumentsResultDto>.Success(new UploadArchiveDocumentsResultDto(processedCount, results));
    }

    private static void ApplyComputation(StyleProfile profile, StyleProfileComputation computed)
    {
        profile.ToneSummary = computed.ToneSummary;
        profile.AvgParagraphLength = computed.AvgParagraphLength;
        profile.OpenerPatterns = computed.OpenerPatterns.ToList();
        profile.CloserPatterns = computed.CloserPatterns.ToList();
        profile.RecurringKeywords = computed.RecurringKeywords.ToList();
        profile.QuoteUsage = computed.QuoteUsage;
    }

    private async Task<UploadedDocumentResultDto> ProcessFileAsync(UploadedDocument file, int actorUserId, CancellationToken cancellationToken)
    {
        DocumentTextExtractionResult extraction = await _textExtractor.ExtractAsync(file.Content, file.ContentType, file.FileName, cancellationToken);

        if (extraction.Status != DocumentTextExtractionStatus.Success)
        {
            return new UploadedDocumentResultDto(null, null, null, file.FileName, extraction.Reason ?? NoExtractableTextReason, null);
        }

        var text = extraction.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new UploadedDocumentResultDto(null, null, null, file.FileName, NoExtractableTextReason, null);
        }

        var title = Path.GetFileNameWithoutExtension(file.FileName);
        var entry = new ArchiveEntry
        {
            Title = title,
            BodyText = text,
            SourceFile = file.FileName,
            CreatedBy = actorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        _dbContext.Add(entry);

        var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return new UploadedDocumentResultDto(entry.Id, entry.Title, wordCount, null, null, null);
    }

    private async Task RecomputeStyleProfileAsync(int actorUserId, CancellationToken cancellationToken)
    {
        List<ArchiveEntry> allEntries = await _queryExecutor.ToListAsync(_dbContext.ArchiveEntries, cancellationToken);
        StyleProfileComputation? computed = StyleProfileRecalculator.Recompute(allEntries.Select(e => e.BodyText).ToList());
        if (computed is null)
        {
            return;
        }

        List<StyleProfile> profiles = await _queryExecutor.ToListAsync(_dbContext.StyleProfiles, cancellationToken);
        StyleProfile? existing = profiles.FirstOrDefault();

        if (existing is not null)
        {
            ApplyComputation(existing, computed);
        }
        else
        {
            var created = new StyleProfile { CreatedBy = actorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            ApplyComputation(created, computed);
            _dbContext.Add(created);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
