using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Ping;

/// <summary>Handles <see cref="GetPingQuery"/>.</summary>
public sealed class GetPingQueryHandler : IRequestHandler<GetPingQuery, Result<PingResponse>>
{
    /// <summary>Produces a static pong response with the current UTC server time.</summary>
    /// <param name="request">The incoming query (carries no parameters).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>A successful <see cref="Result{T}"/> wrapping a <see cref="PingResponse"/>.</returns>
    public Task<Result<PingResponse>> Handle(GetPingQuery request, CancellationToken cancellationToken)
    {
        PingResponse response = new("pong", DateTime.UtcNow);
        return Task.FromResult(Result<PingResponse>.Success(response));
    }
}
