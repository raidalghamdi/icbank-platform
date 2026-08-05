namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/media</c>.</summary>
/// <param name="DataBase64">The base64-encoded file content, optionally with a leading <c>data:...,</c> URI prefix.</param>
/// <param name="ContentType">The MIME content type; defaults to <c>image/png</c> when omitted.</param>
/// <param name="CaptionAr">An optional Arabic caption.</param>
/// <param name="DisplayOrder">The display sort order; defaults to <c>0</c>.</param>
public sealed record UploadShorfahSectionMediaRequest(string DataBase64, string? ContentType, string? CaptionAr, int? DisplayOrder);
