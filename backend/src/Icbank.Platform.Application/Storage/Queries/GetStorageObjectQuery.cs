using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Storage.Queries;

/// <summary>
/// Ports <c>GET /storage/objects/*path</c> (API-SURFACE.md §4). Streams a stored media object,
/// gated by a hardened path-prefix allowlist and traversal-safe normalization
/// (<c>ISafeStoragePathValidator</c>, closes SEC-17).
/// </summary>
/// <param name="RawPath">The untrusted, client-supplied path tail.</param>
public sealed record GetStorageObjectQuery(string RawPath) : IRequest<Result<StoredObject>>;
