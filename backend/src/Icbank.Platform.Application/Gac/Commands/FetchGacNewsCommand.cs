using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Queries every enabled <see cref="Common.Interfaces.INewsSourceProvider"/> for the configured
/// search terms and ingests whatever comes back. Intended to be driven by the same external cron
/// that already calls <c>social-feed/ingest</c>.
/// </summary>
/// <param name="Terms">Overrides the configured search terms for this run; null uses configuration.</param>
/// <param name="WithinDays">Overrides the configured lookback window for this run; null uses configuration.</param>
public sealed record FetchGacNewsCommand(IReadOnlyList<string>? Terms, int? WithinDays)
    : IRequest<Result<FetchGacNewsResult>>;
