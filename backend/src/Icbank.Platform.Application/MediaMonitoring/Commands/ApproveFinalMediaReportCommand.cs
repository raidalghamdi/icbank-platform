using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Approves a reviewed weekly media report: the draft becomes the immutable archived report that
/// the PDF export and the archive list serve.
/// </summary>
/// <param name="ActorUserId">The id of the approver.</param>
/// <param name="ReportId">The draft report id.</param>
/// <param name="ApproverName">The approver's display name, recorded on the report.</param>
public sealed record ApproveFinalMediaReportCommand(int ActorUserId, int ReportId, string? ApproverName)
    : IRequest<Result<FinalMediaReportDto>>
{
    /// <summary>The error returned when the report is already approved.</summary>
    public const string AlreadyApprovedError = "التقرير معتمد مسبقاً";
}
