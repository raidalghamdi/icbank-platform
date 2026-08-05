using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Shared entity-to-DTO mapping for <see cref="MediaReport"/>, used by every media-report query/command handler.</summary>
public static class MediaReportMapper
{
    /// <summary>Maps a <see cref="MediaReport"/> entity to its read model.</summary>
    /// <param name="report">The entity to map.</param>
    /// <returns>The mapped <see cref="MediaReportDto"/>.</returns>
    public static MediaReportDto ToDto(MediaReport report) => new(
        report.Id,
        report.Title,
        report.ReportType.ToString(),
        report.Audience.ToString(),
        report.DateFrom,
        report.DateTo,
        report.Sources,
        report.ExecutiveSummary,
        report.ContentMd,
        report.OverallTone,
        report.Status.ToString(),
        report.CreatedAt);
}
