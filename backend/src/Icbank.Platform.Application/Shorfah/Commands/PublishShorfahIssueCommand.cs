using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues/:id/publish</c> (API-SURFACE.md §19, BUSINESS-RULES.md §1.1,
/// §1.7). Admin-only. Hard precondition: at least one section must be both
/// <c>approved</c> and <c>IncludeInPdf=true</c>. On success, fans out a "published" notification
/// to every user in the system (matching the Node source's <c>getAllUsers()</c>, no
/// role/department/opt-in filtering). Fan-out failures never fail the publish response, matching
/// the Node source's try/catch-and-log-only behaviour around the fan-out loop.
/// </summary>
/// <param name="ActorUserId">The publishing admin's id.</param>
/// <param name="IssueId">The issue being published.</param>
public sealed record PublishShorfahIssueCommand(int ActorUserId, int IssueId) : IRequest<Result<ShorfahIssueDto>>;
