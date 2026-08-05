using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Fetches a single prompt framework by id (<c>GET /prompts/:id</c>).</summary>
/// <param name="FrameworkId">The framework id.</param>
public sealed record GetPromptFrameworkByIdQuery(int FrameworkId) : IRequest<Result<PromptFrameworkDto>>;
