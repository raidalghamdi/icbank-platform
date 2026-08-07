using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Renders a final report to PDF (<c>POST /final-media-reports/:id/export-pdf</c>). Closes
/// DEFECT-LOG.md SEC-02: the Node source ran this unauthenticated Puppeteer resource-cost
/// endpoint with no authentication at all; this port requires <c>media_monitoring:view</c>.
/// </summary>
/// <param name="ReportId">The report id to export.</param>
public sealed record ExportFinalMediaReportPdfCommand(int ReportId) : IRequest<Result<byte[]>>;
