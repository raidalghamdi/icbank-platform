namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="AiYearController.GetUploadUrlAsync"/>.</summary>
/// <param name="Name">The client-supplied original file name.</param>
/// <param name="ContentType">The optional MIME content type.</param>
/// <param name="ActivationId">The activation this media will belong to.</param>
/// <param name="Month">The calendar month (1-12) folder segment.</param>
/// <param name="FileSize">The optional file size in bytes.</param>
public sealed record AiYearUploadUrlRequest(string Name, string? ContentType, int ActivationId, int Month, long? FileSize);
