using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Mints a short-lived, single-use download token scoped to one Shorfah issue's PDF (GAP 2 --
/// FRONTEND-WIRING-NOTES.md §4). The Shorfah PDF preview (<c>GET .../pdf</c>) and binary download
/// (<c>GET .../pdf.pdf</c>) endpoints share one token, matching the frontend's existing behaviour
/// of treating "preview" and "download" as the same underlying document with a different
/// <c>preview</c> query flag -- see <see cref="IssueShorfahIssueDownloadTokenCommandHandler"/>.
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id, recorded on the minted token for forensic audit.</param>
/// <param name="IssueId">The Shorfah issue id the token is scoped to.</param>
public sealed record IssueShorfahIssueDownloadTokenCommand(int ActorUserId, int IssueId) : IRequest<Result<string>>;
