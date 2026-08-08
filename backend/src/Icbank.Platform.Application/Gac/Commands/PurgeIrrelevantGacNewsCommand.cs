using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>Removes stored news items that are not about competition policy.</summary>
/// <remarks>
/// A one-off maintenance command for rows ingested before the relevance filter existed, and a
/// safety valve for whenever the filter vocabulary is tightened.
/// </remarks>
public sealed record PurgeIrrelevantGacNewsCommand : IRequest<Result<PurgeIrrelevantGacNewsResult>>;
