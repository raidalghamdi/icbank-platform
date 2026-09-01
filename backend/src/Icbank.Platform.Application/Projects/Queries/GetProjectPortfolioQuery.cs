using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Projects.Queries;

/// <summary>Reads the whole tracked project portfolio, already scored against its schedule.</summary>
public sealed record GetProjectPortfolioQuery : IRequest<Result<ProjectPortfolioDto>>;
