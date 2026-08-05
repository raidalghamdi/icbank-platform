using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Ping;

/// <summary>
/// Query proving the full request pipeline (validation behaviour, MediatR dispatch, Result
/// mapping) works end-to-end without needing a database (R-BE-003).
/// </summary>
public sealed record GetPingQuery : IRequest<Result<PingResponse>>;
