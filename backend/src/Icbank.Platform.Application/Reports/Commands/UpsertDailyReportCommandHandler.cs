using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Reports;
using MediatR;

namespace Icbank.Platform.Application.Reports.Commands;

/// <summary>
/// Handles <see cref="UpsertDailyReportCommand"/>. Ports the Node select-then-branch upsert
/// (BUSINESS-RULES.md §6) as-is (not race-safe under concurrent same-date submissions — same
/// caveat as the source; DOMAIN-PORT-NOTES.md §2.7 adds a unique index on <c>report_date</c> so a
/// genuine race now surfaces as a conflict rather than silently duplicating rows). This endpoint
/// is reached only via the API-key-authenticated n8n ingestion path (no interactive user
/// principal exists), so it deliberately does not write to the human-actor <c>AuditLogEntry</c>
/// table (which has an enforced FK to <c>Users</c>) — see WAVE1-PORT-NOTES.md.
/// </summary>
public sealed class UpsertDailyReportCommandHandler : IRequestHandler<UpsertDailyReportCommand, Result<DailyReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="UpsertDailyReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock, used for the n8n <c>_receivedAt</c> provenance stamp.</param>
    public UpsertDailyReportCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<DailyReportDto>> Handle(UpsertDailyReportCommand request, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(request.ReportDate, out DateOnly reportDate))
        {
            return Result<DailyReportDto>.Failure("Invalid reportDate.");
        }

        var reportDataJson = request.ApplyN8NNormalization
            ? N8NPayloadNormalizer.Normalize(request.ReportDataJson, _dateTimeProvider.UtcNow)
            : request.ReportDataJson;

        DailyReport? existing = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.DailyReports.Where(r => r.ReportDate == reportDate), cancellationToken);

        DailyReport report;
        if (existing is not null)
        {
            existing.ReportDataJson = reportDataJson;
            report = existing;
        }
        else
        {
            report = new DailyReport { ReportDate = reportDate, ReportDataJson = reportDataJson, CreatedBy = "n8n" };
            _dbContext.Add(report);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<DailyReportDto>.Success(new DailyReportDto(report.Id, report.ReportDate, report.ReportDataJson));
    }
}
