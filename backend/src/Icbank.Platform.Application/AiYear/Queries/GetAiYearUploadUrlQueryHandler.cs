using System.Globalization;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Handles <see cref="GetAiYearUploadUrlQuery"/>.</summary>
public sealed class GetAiYearUploadUrlQueryHandler : IRequestHandler<GetAiYearUploadUrlQuery, Result<PresignedUpload>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IObjectUploadUrlIssuer _uploadUrlIssuer;

    /// <summary>Initializes a new instance of the <see cref="GetAiYearUploadUrlQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="uploadUrlIssuer">The presigned-upload-URL port.</param>
    public GetAiYearUploadUrlQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IObjectUploadUrlIssuer uploadUrlIssuer)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _uploadUrlIssuer = uploadUrlIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<PresignedUpload>> Handle(GetAiYearUploadUrlQuery request, CancellationToken cancellationToken)
    {
        var activationExists = await _queryExecutor.AnyAsync(
            _dbContext.AiYearActivations.Where(a => a.Id == request.ActivationId), cancellationToken);
        if (!activationExists)
        {
            return Result<PresignedUpload>.Failure("التفعيل المرتبط غير موجود");
        }

        var folderPrefix = string.Create(CultureInfo.InvariantCulture, $"ai-year/2026/{request.Month}/{request.ActivationId}/");
        PresignedUpload upload = await _uploadUrlIssuer.IssueAsync(folderPrefix, request.Name, request.ContentType, cancellationToken);
        return Result<PresignedUpload>.Success(upload);
    }
}
