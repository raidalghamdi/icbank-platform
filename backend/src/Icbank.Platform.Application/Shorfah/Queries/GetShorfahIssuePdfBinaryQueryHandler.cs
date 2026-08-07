using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Handles <see cref="GetShorfahIssuePdfBinaryQuery"/>. Delegates HTML assembly to
/// <see cref="GetShorfahIssuePdfHtmlQueryHandler"/>'s exact logic via
/// <see cref="GetShorfahIssuePdfHtmlQuery"/>, then renders through <see cref="IShorfahIssuePdfRenderer"/>
/// -- ports <c>shorfah.ts:705-828</c> without duplicating the section-fetch/HTML-assembly logic a
/// second time (BUSINESS-RULES.md §1.9 flags the Node source's own duplication here as "a strong
/// candidate for extracting a shared helper during the port", which this handler does).
/// </summary>
public sealed class GetShorfahIssuePdfBinaryQueryHandler : IRequestHandler<GetShorfahIssuePdfBinaryQuery, Result<byte[]>>
{
    private readonly ISender _sender;
    private readonly IShorfahIssuePdfRenderer _renderer;

    /// <summary>Initializes a new instance of the <see cref="GetShorfahIssuePdfBinaryQueryHandler"/> class.</summary>
    /// <param name="sender">The MediatR sender used to reuse the HTML-assembly query.</param>
    /// <param name="renderer">The PDF rendering port.</param>
    public GetShorfahIssuePdfBinaryQueryHandler(ISender sender, IShorfahIssuePdfRenderer renderer)
    {
        _sender = sender;
        _renderer = renderer;
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> Handle(GetShorfahIssuePdfBinaryQuery request, CancellationToken cancellationToken)
    {
        Result<string> htmlResult = await _sender.Send(new GetShorfahIssuePdfHtmlQuery(request.IssueId, request.Preview), cancellationToken);
        if (!htmlResult.IsSuccess)
        {
            return Result<byte[]>.Failure(htmlResult.Error!);
        }

        var bytes = await _renderer.RenderAsync(htmlResult.Value!, cancellationToken);
        return Result<byte[]>.Success(bytes);
    }
}
