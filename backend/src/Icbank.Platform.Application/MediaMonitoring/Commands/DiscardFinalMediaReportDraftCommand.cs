using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Discards a report that is still a draft. Approved reports can never be deleted.</summary>
/// <param name="ReportId">The draft report id.</param>
public sealed record DiscardFinalMediaReportDraftCommand(int ReportId) : IRequest<Result<bool>>;
