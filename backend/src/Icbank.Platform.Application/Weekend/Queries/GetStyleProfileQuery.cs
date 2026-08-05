using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /week-start/style-profile</c> (API-SURFACE.md §8).</summary>
public sealed record GetStyleProfileQuery : IRequest<Result<StyleProfileDto?>>;
