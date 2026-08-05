using Asp.Versioning;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Application.Storage.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>GET /storage/objects/*path</c> (API-SURFACE.md §4). The Node source mounted this
/// router twice — once pre-auth, once post-auth — making it "effectively always public"
/// regardless of the optional bearer-token layer (which itself did nothing when unset). This port
/// closes that gap by requiring authentication unconditionally (any authenticated user) in
/// addition to the traversal-safe path validator and prefix allowlist — see
/// WAVE1-PORT-NOTES.md.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/storage")]
[Authorize]
public sealed class StorageController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="StorageController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch the storage query.</param>
    public StorageController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Streams a stored media object by its path tail, subject to a hardened path-prefix allowlist.</summary>
    /// <param name="path">The path tail relative to the storage root.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the object bytes, 403 if the path fails validation/allowlist, or 404 if not found.</returns>
    [HttpGet("objects/{*path}")]
    public async Task<ActionResult> GetObjectAsync(string path, CancellationToken cancellationToken)
    {
        Result<StoredObject> result = await _sender.Send(new GetStorageObjectQuery(path ?? string.Empty), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error == "not_found"
                ? NotFound(new { error = "Object not found" })
                : StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden: path not in allowed storage prefixes" });
        }

        return File(result.Value!.Content, result.Value.ContentType);
    }
}
