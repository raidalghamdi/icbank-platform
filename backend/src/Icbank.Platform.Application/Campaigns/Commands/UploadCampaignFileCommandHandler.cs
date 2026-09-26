using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>Handles <see cref="UploadCampaignFileCommand"/>.</summary>
public sealed class UploadCampaignFileCommandHandler : IRequestHandler<UploadCampaignFileCommand, Result<CampaignFileDto>>
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "video/mp4",
        "application/zip",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    private readonly IObjectStorageWriter _storage;

    /// <summary>Initializes a new instance of the <see cref="UploadCampaignFileCommandHandler"/> class.</summary>
    /// <param name="storage">The object storage writer.</param>
    public UploadCampaignFileCommandHandler(IObjectStorageWriter storage)
    {
        _storage = storage;
    }

    /// <inheritdoc />
    public async Task<Result<CampaignFileDto>> Handle(UploadCampaignFileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var contentType = (request.ContentType ?? string.Empty).Split(';')[0].Trim();
        if (!AllowedTypes.Contains(contentType))
        {
            return Result<CampaignFileDto>.Failure(UploadCampaignFileCommand.TypeError);
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.DataBase64 ?? string.Empty);
        }
        catch (FormatException)
        {
            return Result<CampaignFileDto>.Failure(UploadCampaignFileCommand.InvalidFileError);
        }

        if (bytes.Length == 0 || bytes.Length > UploadCampaignFileCommand.MaxBytes)
        {
            return Result<CampaignFileDto>.Failure(UploadCampaignFileCommand.InvalidFileError);
        }

        var folder = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"designs/campaigns/{request.CampaignId}/");
        var path = await _storage.SaveAsync(folder, bytes, contentType, cancellationToken);
        var name = Path.GetFileName(request.FileName ?? string.Empty);
        return Result<CampaignFileDto>.Success(new CampaignFileDto(path, name.Length == 0 ? "file" : name, contentType, bytes.Length));
    }
}
