using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Lists active prompt frameworks, optionally filtered by category/kind (<c>GET /prompts</c>).</summary>
/// <param name="Query">The pagination parameters.</param>
/// <param name="Category">Optional category filter.</param>
/// <param name="Kind">Optional kind filter (<c>framework</c> or <c>template</c>).</param>
public sealed record ListPromptFrameworksQuery(PagedQuery Query, string? Category, string? Kind)
    : IRequest<Result<PagedResult<PromptFrameworkDto>>>;
