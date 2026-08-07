using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Handles <see cref="GetDesignAssetUploadUrlQuery"/>.</summary>
public sealed class GetDesignAssetUploadUrlQueryHandler : IRequestHandler<GetDesignAssetUploadUrlQuery, Result<PresignedUpload>>
{
    private readonly IObjectUploadUrlIssuer _uploadUrlIssuer;

    /// <summary>Initializes a new instance of the <see cref="GetDesignAssetUploadUrlQueryHandler"/> class.</summary>
    /// <param name="uploadUrlIssuer">The presigned-upload-URL port.</param>
    public GetDesignAssetUploadUrlQueryHandler(IObjectUploadUrlIssuer uploadUrlIssuer)
    {
        _uploadUrlIssuer = uploadUrlIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<PresignedUpload>> Handle(GetDesignAssetUploadUrlQuery request, CancellationToken cancellationToken)
    {
        var folderPrefix = $"designs/{request.Folder}/";
        PresignedUpload upload = await _uploadUrlIssuer.IssueAsync(folderPrefix, request.FileName, request.ContentType, cancellationToken);
        return Result<PresignedUpload>.Success(upload);
    }
}
