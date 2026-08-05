using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>PATCH /shorfah/issues/:id</c> (API-SURFACE.md §19). Admin-only. Every property is a
/// nullable partial-update field; a <c>null</c> means "leave unchanged" (matches the Node
/// source's <c>if (req.body[k] !== undefined)</c> semantics). <see cref="Status"/> updates via
/// this route bypass the dedicated transition endpoints entirely, matching the Node source's
/// permissive <c>patch.status = req.body.status</c> with zero state-machine validation --
/// flagged as a deliberate behaviour change in WAVE4A-PORT-NOTES.md (this port validates the
/// requested status transition through the same state machine the dedicated transition
/// endpoints use, rather than allowing an arbitrary status write).
/// </summary>
/// <param name="ActorUserId">The editing admin's id.</param>
/// <param name="IssueId">The issue being edited.</param>
/// <param name="TitleAr">The replacement title, or <c>null</c> to leave unchanged.</param>
/// <param name="SubtitleAr">The replacement subtitle, or <c>null</c> to leave unchanged.</param>
/// <param name="EditorLetter">The replacement editor letter, or <c>null</c> to leave unchanged.</param>
/// <param name="CoverImageUrl">The replacement cover image URL, or <c>null</c> to leave unchanged.</param>
/// <param name="Status">The replacement status, or <c>null</c> to leave unchanged. Must be a legal transition from the current status.</param>
/// <param name="ContributionsOpenAt">The replacement contributions-open timestamp, or <c>null</c> to leave unchanged.</param>
/// <param name="ContributionsCloseAt">The replacement contributions-close timestamp, or <c>null</c> to leave unchanged.</param>
public sealed record UpdateShorfahIssueCommand(
    int ActorUserId,
    int IssueId,
    string? TitleAr,
    string? SubtitleAr,
    string? EditorLetter,
    string? CoverImageUrl,
    string? Status,
    DateTimeOffset? ContributionsOpenAt,
    DateTimeOffset? ContributionsCloseAt) : IRequest<Result<ShorfahIssueDto>>;
