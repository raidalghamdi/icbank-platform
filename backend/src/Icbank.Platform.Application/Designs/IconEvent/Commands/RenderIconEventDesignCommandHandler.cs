using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Handles <see cref="RenderIconEventDesignCommand"/>. Ports BUSINESS-RULES.md §7.5's
/// render pipeline. Rate limited and audited per the task's "image-generation endpoints are an
/// external-cost abuse vector" instruction -- the Node source had no rate limit on this route at
/// all.
/// </summary>
public sealed class RenderIconEventDesignCommandHandler : IRequestHandler<RenderIconEventDesignCommand, Result<RenderIconEventDesignResultDto>>
{
    private const int HdScaleFactor = 3;
    private const int UltraScaleFactor = 4;
    private const string ImagePngContentType = "image/png";
    private const string StorageFolderPrefix = "designs/icon-event/";

    private readonly IIconEventImageRenderer _imageRenderer;
    private readonly IObjectStorageWriter _storageWriter;
    private readonly IDesignGenerationRateLimiter _rateLimiter;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RenderIconEventDesignCommandHandler"/> class.</summary>
    /// <param name="imageRenderer">The HTML-to-image rendering port.</param>
    /// <param name="storageWriter">The object-storage write port.</param>
    /// <param name="rateLimiter">The per-user generation rate limiter.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public RenderIconEventDesignCommandHandler(
        IIconEventImageRenderer imageRenderer, IObjectStorageWriter storageWriter, IDesignGenerationRateLimiter rateLimiter, IAuditLogService auditLogService)
    {
        _imageRenderer = imageRenderer;
        _storageWriter = storageWriter;
        _rateLimiter = rateLimiter;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<RenderIconEventDesignResultDto>> Handle(RenderIconEventDesignCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.ActorUserId))
        {
            return Result<RenderIconEventDesignResultDto>.Failure("تجاوزت حد التوليد المؤقت، انتظر دقيقة وحاول مجدداً.");
        }

        if (!IconEventSizeCatalog.TryParse(request.Size, out IconEventSizePreset size))
        {
            return Result<RenderIconEventDesignResultDto>.Failure("مقاس غير معروف.");
        }

        var isUltra = string.Equals(request.Quality, "ultra", StringComparison.OrdinalIgnoreCase);
        var scaleFactor = isUltra ? UltraScaleFactor : HdScaleFactor;
        (var width, var height) = IconEventSizeCatalog.Dimensions(size);

        var bytes = await _imageRenderer.RenderAsync(request.Html, size, isUltra, cancellationToken);
        var objectPath = await _storageWriter.SaveAsync(StorageFolderPrefix, bytes, ImagePngContentType, cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.icon_event.render", "IconEventDesign", objectPath, before: null, after: new { size = request.Size }, cancellationToken);

        var qualityLabel = isUltra ? "ultra (4x)" : "hd (3x)";
        var result = new RenderIconEventDesignResultDto(objectPath, request.Size, width * scaleFactor, height * scaleFactor, qualityLabel);
        return Result<RenderIconEventDesignResultDto>.Success(result);
    }
}
