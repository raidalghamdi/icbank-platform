using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Queries;

/// <summary>Ports <c>GET /designs/icon-event/icons</c> (API-SURFACE.md §18).</summary>
public sealed record ListIconEventIconsQuery : IRequest<Result<IconEventIconCatalogDto>>;
