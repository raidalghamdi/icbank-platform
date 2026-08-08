using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Handles <see cref="GenerateIconEventDesignCommand"/>. Ports BUSINESS-RULES.md §7.4's
/// anti-hallucination extraction pipeline: calls the AI extractor, applies every code-enforced
/// post-processing rule (stats hallucination backstop, headline precedence, contact
/// extraction-wins-over-AI, subtitle preservation, layout diversity/typography guarantee, hardcoded
/// teal color scheme), and falls back to a fully deterministic local variant set if the AI call
/// throws -- matching the Node source's guaranteed-output contract exactly.
/// </summary>
public sealed class GenerateIconEventDesignCommandHandler
    : IRequestHandler<GenerateIconEventDesignCommand, Result<GenerateIconEventDesignResultDto>>
{
    private const int MaxStats = 3;
    private const int FallbackHeadlineLength = 60;

    private readonly IIconEventDesignExtractor _extractor;
    private readonly IIconEventHtmlRenderer _htmlRenderer;
    private readonly IDesignGenerationRateLimiter _rateLimiter;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventDesignCommandHandler"/> class.</summary>
    /// <param name="extractor">The AI-backed (or placeholder) extraction port.</param>
    /// <param name="htmlRenderer">The HTML rendering port.</param>
    /// <param name="rateLimiter">The per-user generation rate limiter.</param>
    /// <param name="auditLogService">The audit-log port.</param>
    public GenerateIconEventDesignCommandHandler(
        IIconEventDesignExtractor extractor, IIconEventHtmlRenderer htmlRenderer, IDesignGenerationRateLimiter rateLimiter, IAuditLogService auditLogService)
    {
        _extractor = extractor;
        _htmlRenderer = htmlRenderer;
        _rateLimiter = rateLimiter;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<GenerateIconEventDesignResultDto>> Handle(GenerateIconEventDesignCommand request, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryConsume(request.ActorUserId))
        {
            return Result<GenerateIconEventDesignResultDto>.Failure("تجاوزت حد التوليد المؤقت، انتظر دقيقة وحاول مجدداً.");
        }

        // The three style previews are always drawn at the same preset; real output sizes are
        // chosen in a later step and rendered by the studio endpoint.
        IconEventSizePreset size = IconEventSizeCatalog.TryParse(request.Size, out IconEventSizePreset requested)
            ? requested
            : IconEventSizePreset.DesktopHd;
        var inputText = string.Join(' ', new[] { request.RawData, request.Headline, request.Subtitle }.Where(s => !string.IsNullOrEmpty(s)));
        var hasNumbers = inputText.Any(char.IsDigit);

        GenerateIconEventDesignResultDto result;
        try
        {
            var prompt = IconEventPromptBuilder.Build(
                request.RawData,
                request.Headline,
                request.Subtitle,
                request.Department,
                request.Hashtag,
                request.Date,
                request.Time,
                request.Location,
                request.EventType);
            IconEventExtractionResultDto extraction = await _extractor.ExtractAsync(prompt, cancellationToken);
            result = IconEventVariantAssembler.BuildFromAi(extraction, request, hasNumbers, size, _htmlRenderer);
        }
        catch (InvalidOperationException)
        {
            result = IconEventVariantAssembler.BuildFallback(request, size, _htmlRenderer);
        }

        await _auditLogService.RecordAsync(
            request.ActorUserId, "design.icon_event.generate", "IconEventDesign", size.ToString(), before: null, after: new { result.Count }, cancellationToken);
        return Result<GenerateIconEventDesignResultDto>.Success(result);
    }
}
