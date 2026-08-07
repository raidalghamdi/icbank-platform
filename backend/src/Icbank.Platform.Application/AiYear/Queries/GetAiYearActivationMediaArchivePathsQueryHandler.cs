using System.Text.RegularExpressions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Handles <see cref="GetAiYearActivationMediaArchivePathsQuery"/>.</summary>
public sealed partial class GetAiYearActivationMediaArchivePathsQueryHandler
    : IRequestHandler<GetAiYearActivationMediaArchivePathsQuery, Result<AiYearActivationMediaArchiveDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetAiYearActivationMediaArchivePathsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetAiYearActivationMediaArchivePathsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearActivationMediaArchiveDto>> Handle(
        GetAiYearActivationMediaArchivePathsQuery request, CancellationToken cancellationToken)
    {
        AiYearActivation? activation = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == request.ActivationId), cancellationToken);
        if (activation is null)
        {
            return Result<AiYearActivationMediaArchiveDto>.Failure("التفعيل غير موجود");
        }

        List<AiYearMedia> media = await _queryExecutor.ToListAsync(
            _dbContext.AiYearMedia.Where(m => m.ActivationId == request.ActivationId).OrderBy(m => m.SortOrder), cancellationToken);
        if (media.Count == 0)
        {
            return Result<AiYearActivationMediaArchiveDto>.Failure("لا توجد صور لهذا التفعيل");
        }

        var entries = media.Select(m => new AiYearArchiveEntryDto(SanitizeEntryName(m), m.ObjectPath)).ToList();
        return Result<AiYearActivationMediaArchiveDto>.Success(new AiYearActivationMediaArchiveDto(activation.Title, entries));
    }

    private static string SanitizeEntryName(AiYearMedia media)
    {
        var rawName = string.IsNullOrEmpty(media.FileName) ? $"file-{media.Id}.bin" : media.FileName;
        var baseName = Path.GetFileName(rawName);
        var sanitized = UnsafeEntryNameCharsRegex().Replace(baseName, "_");
        return string.IsNullOrEmpty(sanitized) ? $"file-{media.Id}.bin" : sanitized;
    }

    [GeneratedRegex(@"[^\w.\-]")]
    private static partial Regex UnsafeEntryNameCharsRegex();
}
