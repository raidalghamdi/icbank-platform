using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/media</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:559-597</c>.</summary>
/// <param name="ActorUserId">The authenticated caller's id.</param>
/// <param name="SectionId">The section the media is attached to.</param>
/// <param name="DataBase64">The base64-encoded file content, optionally with a leading <c>data:...,</c> URI prefix.</param>
/// <param name="ContentType">The MIME content type; defaults to <c>image/png</c> when omitted.</param>
/// <param name="CaptionAr">An optional Arabic caption.</param>
/// <param name="DisplayOrder">The display sort order; defaults to <c>0</c>.</param>
public sealed record UploadShorfahSectionMediaCommand(
    int ActorUserId, int SectionId, string DataBase64, string? ContentType, string? CaptionAr, int? DisplayOrder)
    : IRequest<Result<ShorfahSectionMediaDto>>;
