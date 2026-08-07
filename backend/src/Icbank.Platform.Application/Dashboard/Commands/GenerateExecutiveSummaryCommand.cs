using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Dashboard.Commands;

/// <summary>Ports <c>POST /dashboard/ai-summary</c> (API-SURFACE.md §6, BUSINESS-RULES.md §9). Takes no input — reads recent DB rows and summarizes.</summary>
public sealed record GenerateExecutiveSummaryCommand : IRequest<Result<ExecutiveSummaryDto>>;
