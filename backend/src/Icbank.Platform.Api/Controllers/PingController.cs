using Asp.Versioning;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Ping;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Proves the versioned API surface, MediatR dispatch, and Result mapping all work end-to-end
/// (R-BE-030, R-BE-003).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ping")]
public sealed class PingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="PingController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch the ping query.</param>
    public PingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns a static acknowledgement proving the API pipeline is reachable end-to-end.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with a <see cref="PingResponse"/> payload.</returns>
    [HttpGet]
    public async Task<ActionResult<PingResponse>> GetAsync(CancellationToken cancellationToken)
    {
        Result<PingResponse> result = await _sender.Send(new GetPingQuery(), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
    }
}
