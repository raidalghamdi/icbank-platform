using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /wk2-data</c> (API-SURFACE.md §9, BUSINESS-RULES.md §2.4).</summary>
public sealed record GetWk2DataQuery : IRequest<Result<Wk2DataDto>>;
