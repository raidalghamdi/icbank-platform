using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>POST /weekend-places/upload-url</c> (API-SURFACE.md §9). Admin-only.</summary>
/// <param name="FileName">The client-supplied original file name.</param>
/// <param name="ContentType">The optional MIME content type.</param>
public sealed record GetWeekendPlaceUploadUrlQuery(string FileName, string? ContentType) : IRequest<Result<PresignedUpload>>;
