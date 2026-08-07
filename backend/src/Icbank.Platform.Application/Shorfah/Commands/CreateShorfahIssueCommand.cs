using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues</c> (API-SURFACE.md §19, BUSINESS-RULES.md §1.1). Admin-only.
/// Auto-assigns <c>IssueNo</c> as <c>max(IssueNo)+1</c> when not supplied, and always seeds the
/// 13 canonical sections synchronously (the Node source swallowed seeding failures silently --
/// this port does not, see the handler's remarks).
/// </summary>
/// <param name="ActorUserId">The creating admin's id.</param>
/// <param name="IssueNo">The explicit issue number, or <c>null</c> to auto-assign.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="SubtitleAr">The optional Arabic subtitle.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="ContributionsOpenAt">The optional UTC timestamp contributions open.</param>
/// <param name="ContributionsCloseAt">The optional UTC timestamp contributions close.</param>
/// <param name="EditorLetter">The optional editor's letter content.</param>
public sealed record CreateShorfahIssueCommand(
    int ActorUserId,
    int? IssueNo,
    string TitleAr,
    string? SubtitleAr,
    int Month,
    int Year,
    DateTimeOffset? ContributionsOpenAt,
    DateTimeOffset? ContributionsCloseAt,
    string? EditorLetter) : IRequest<Result<ShorfahIssueDto>>;
