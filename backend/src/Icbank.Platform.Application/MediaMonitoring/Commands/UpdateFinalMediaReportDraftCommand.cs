using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Saves the Review &amp; Edit stage of a weekly media report: the edited title and content, and
/// the reviewer's section layout. Only a draft can be saved; an approved report is immutable.
/// </summary>
/// <param name="ActorUserId">The id of the reviewer saving the draft.</param>
/// <param name="ReportId">The draft report id.</param>
/// <param name="Title">The edited report title.</param>
/// <param name="Draft">The edited report content.</param>
/// <param name="LayoutJson">The section layout, Contact Center figures and reviewer-added sections.</param>
public sealed record UpdateFinalMediaReportDraftCommand(
    int ActorUserId,
    int ReportId,
    string Title,
    FinalReportDraftDto Draft,
    string? LayoutJson) : IRequest<Result<FinalMediaReportDetailDto>>
{
    /// <summary>The error returned when the report does not exist.</summary>
    public const string NotFoundError = "التقرير غير موجود";

    /// <summary>The error returned when the report is already approved.</summary>
    public const string LockedError = "التقرير معتمد ومحفوظ في الأرشيف — لا يمكن تعديله.";

    /// <summary>The error returned when the title is empty.</summary>
    public const string EmptyTitleError = "عنوان التقرير مطلوب";
}
