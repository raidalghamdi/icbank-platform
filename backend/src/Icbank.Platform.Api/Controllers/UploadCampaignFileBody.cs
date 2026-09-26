namespace Icbank.Platform.Api.Controllers;

/// <summary>The body of the campaign file upload routes.</summary>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The MIME type.</param>
/// <param name="DataBase64">The file bytes, base64 encoded.</param>
public sealed record UploadCampaignFileBody(string FileName, string ContentType, string DataBase64);
