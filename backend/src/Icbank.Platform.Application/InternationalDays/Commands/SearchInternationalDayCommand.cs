using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Ports <c>POST /intl-days/search</c> (API-SURFACE.md §14). The Node source streamed progress
/// over SSE; this port returns a single synchronous result, consistent with how Wave 1 handled
/// the analogous <c>week-start/generate</c> endpoint (no SSE convention exists in this codebase).
/// The dual-provider merge (BUSINESS-RULES.md §4.3) is deliberately NOT ported -- it was dead
/// code in the live search path (<c>secondaryResult</c> always empty, DEFECT-LOG.md ARCH-07);
/// see WAVE2-PORT-NOTES.md.
/// </summary>
/// <param name="Query">The day name to research.</param>
/// <param name="Category">The optional category to tag the result with.</param>
/// <param name="ForceRefresh">Whether to bypass the 7-day cache.</param>
/// <param name="IpAddress">The caller's IP address, for rate limiting and search-history logging.</param>
public sealed record SearchInternationalDayCommand(string Query, string? Category, bool ForceRefresh, string IpAddress)
    : IRequest<Result<SearchInternationalDayResultDto>>;
