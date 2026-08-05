using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Query for <c>GET /shorfah/sla-defaults</c> (BUSINESS-RULES.md §1.5). Ports <c>shorfah.ts:271-274</c>.</summary>
public sealed record ListShorfahSlaDefaultsQuery : IRequest<Result<IReadOnlyList<ShorfahSlaDefaultDto>>>;
