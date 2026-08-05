using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="GetWeekendPlaceUploadUrlQuery"/>.</summary>
public sealed class GetWeekendPlaceUploadUrlQueryHandler : IRequestHandler<GetWeekendPlaceUploadUrlQuery, Result<PresignedUpload>>
{
    private const string WeekendPlacesFolderPrefix = "weekend/";

    private readonly IObjectUploadUrlIssuer _uploadUrlIssuer;

    /// <summary>Initializes a new instance of the <see cref="GetWeekendPlaceUploadUrlQueryHandler"/> class.</summary>
    /// <param name="uploadUrlIssuer">The presigned-upload-URL issuing port.</param>
    public GetWeekendPlaceUploadUrlQueryHandler(IObjectUploadUrlIssuer uploadUrlIssuer)
    {
        _uploadUrlIssuer = uploadUrlIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<PresignedUpload>> Handle(GetWeekendPlaceUploadUrlQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return Result<PresignedUpload>.Failure("fileName مطلوب");
        }

        PresignedUpload upload = await _uploadUrlIssuer.IssueAsync(WeekendPlacesFolderPrefix, request.FileName, request.ContentType, cancellationToken);
        return Result<PresignedUpload>.Success(upload);
    }
}
