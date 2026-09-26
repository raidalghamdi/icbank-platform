using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>Stores a file attached to a campaign (a deliverable, a brief, a report) and returns where it lives.</summary>
/// <param name="CampaignId">The campaign the file belongs to.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The MIME type.</param>
/// <param name="DataBase64">The file bytes, base64 encoded.</param>
public sealed record UploadCampaignFileCommand(int CampaignId, string FileName, string ContentType, string DataBase64)
    : IRequest<Result<CampaignFileDto>>
{
    /// <summary>The largest file accepted, in bytes (15 MB).</summary>
    public const int MaxBytes = 15 * 1024 * 1024;

    /// <summary>The error returned for an empty, unreadable or oversized file.</summary>
    public const string InvalidFileError = "الملف فارغ أو غير صالح أو يتجاوز 15 ميجابايت.";

    /// <summary>The error returned for a file type the platform does not store.</summary>
    public const string TypeError = "نوع الملف غير مدعوم. الأنواع المسموحة: PDF وWord وPowerPoint وExcel والصور والفيديو وZIP.";
}
