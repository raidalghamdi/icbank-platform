using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Ports <c>POST /designs/logos/upload-url</c> and <c>POST /designs/fonts/upload-url</c> (API-SURFACE.md §17).</summary>
/// <param name="FileName">The client-supplied original file name, used only to derive a safe extension (SEC-17: never trusted as a path).</param>
/// <param name="ContentType">The optional MIME content type.</param>
/// <param name="Folder">The storage folder segment, <c>logos</c> or <c>fonts</c>.</param>
public sealed record GetDesignAssetUploadUrlQuery(string FileName, string? ContentType, string Folder) : IRequest<Result<PresignedUpload>>;
