using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>
/// Ports <c>POST /ai-year/upload-url</c> (API-SURFACE.md §13). Validates the MIME allowlist, the
/// 50MB cap, and activation existence exactly as the Node source did, then issues a presigned
/// upload URL under the <c>ai-year/2026/{month}/{activationId}/</c> prefix the media-path regex
/// (<see cref="AiYearMediaPathValidator"/>) expects.
/// </summary>
/// <param name="Name">The client-supplied original file name.</param>
/// <param name="ContentType">The optional MIME content type.</param>
/// <param name="ActivationId">The activation this media will belong to.</param>
/// <param name="Month">The calendar month (1-12) folder segment.</param>
/// <param name="FileSize">The optional file size in bytes, checked against the 50MB cap.</param>
public sealed record GetAiYearUploadUrlQuery(string Name, string? ContentType, int ActivationId, int Month, long? FileSize)
    : IRequest<Result<PresignedUpload>>;
