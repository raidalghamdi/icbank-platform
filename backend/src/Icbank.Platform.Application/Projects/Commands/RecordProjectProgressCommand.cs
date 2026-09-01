using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Projects.Commands;

/// <summary>
/// Records one progress report against a tracked project. Managers report progress repeatedly on
/// the same project, so this appends to the project's history rather than replacing it, and the
/// card's percentage follows the latest report.
/// </summary>
/// <param name="ProjectId">The project to report against.</param>
/// <param name="ProgressPercent">The completion percentage now reached, 0-100.</param>
/// <param name="Note">The progress note explaining what moved.</param>
/// <param name="ReportedBy">The display name of the manager logging the update.</param>
public sealed record RecordProjectProgressCommand(int ProjectId, int ProgressPercent, string Note, string ReportedBy)
    : IRequest<Result<PortfolioProjectDto>>
{
    /// <summary>The failure message returned when the project does not exist or is no longer tracked; the API maps it to 404.</summary>
    public const string ProjectNotFoundError = "المشروع غير موجود";

    /// <summary>The failure message returned when the reported percentage falls outside 0-100.</summary>
    public const string ProgressOutOfRangeError = "نسبة الإنجاز يجب أن تكون بين 0 و 100";

    /// <summary>The failure message returned when the progress note is empty.</summary>
    public const string EmptyNoteError = "نص التحديث مطلوب";
}
