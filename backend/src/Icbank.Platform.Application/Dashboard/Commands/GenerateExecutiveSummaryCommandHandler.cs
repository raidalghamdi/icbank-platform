using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Dashboard.Commands;

/// <summary>Handles <see cref="GenerateExecutiveSummaryCommand"/>, porting BUSINESS-RULES.md §9's data-digest assembly verbatim.</summary>
public sealed class GenerateExecutiveSummaryCommandHandler
    : IRequestHandler<GenerateExecutiveSummaryCommand, Result<ExecutiveSummaryDto>>
{
    private const int RecentActivationCount = 5;
    private const int RecentArchiveCount = 3;
    private const string ActivationCountLinePrefix = "إجمالي تفعيلات عام الذكاء الاصطناعي: ";
    private const string RecentActivationsLinePrefix = "آخر التفعيلات المضافة: ";
    private const string RecentArchiveLinePrefix = "آخر رسائل بداية الأسبوع: ";
    private const string LineSeparator = "، ";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IExecutiveSummaryGenerator _summaryGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="GenerateExecutiveSummaryCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="summaryGenerator">The AI-backed (or template-backed) summary generation port.</param>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    public GenerateExecutiveSummaryCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IExecutiveSummaryGenerator summaryGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _summaryGenerator = summaryGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<ExecutiveSummaryDto>> Handle(GenerateExecutiveSummaryCommand request, CancellationToken cancellationToken)
    {
        List<AiYearActivation> activations = await _queryExecutor.ToListAsync(_dbContext.AiYearActivations, cancellationToken);
        var recentActivationTitles = activations.OrderByDescending(a => a.CreatedAt).Take(RecentActivationCount).Select(a => a.Title).ToList();

        List<ArchiveEntry> archiveEntries = await _queryExecutor.ToListAsync(_dbContext.ArchiveEntries, cancellationToken);
        var recentArchiveTitles = archiveEntries.OrderByDescending(e => e.CreatedAt).Take(RecentArchiveCount).Select(e => e.Title).ToList();

        var digest = BuildDataDigest(activations.Count, recentActivationTitles, recentArchiveTitles);
        var summary = await _summaryGenerator.GenerateAsync(digest, cancellationToken);

        return Result<ExecutiveSummaryDto>.Success(new ExecutiveSummaryDto(summary, _dateTimeProvider.UtcNow));
    }

    private static string BuildDataDigest(int activationCount, List<string> recentActivationTitles, List<string> recentArchiveTitles)
    {
        var lines = new List<string> { ActivationCountLinePrefix + activationCount.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (recentActivationTitles.Count > 0)
        {
            lines.Add(RecentActivationsLinePrefix + string.Join(LineSeparator, recentActivationTitles));
        }

        if (recentArchiveTitles.Count > 0)
        {
            lines.Add(RecentArchiveLinePrefix + string.Join(LineSeparator, recentArchiveTitles));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
