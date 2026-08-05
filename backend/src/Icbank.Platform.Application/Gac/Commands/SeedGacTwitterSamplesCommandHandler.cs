using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Handles <see cref="SeedGacTwitterSamplesCommand"/>. Ports the Node source's 5 hardcoded sample
/// posts (<c>gac.ts:255-350</c>) verbatim, including their fixed <c>externalId</c> idempotency keys.
/// </summary>
public sealed class SeedGacTwitterSamplesCommandHandler : IRequestHandler<SeedGacTwitterSamplesCommand, Result<SeedGacTwitterSamplesResult>>
{
    private const string Account = "GACOMPKSA";

    private static readonly IReadOnlyList<SampleSpec> Samples = new List<SampleSpec>
    {
        new("tw-gac-2026-07-08-launch", "أطلقت #الهيئة_العامة_للمنافسة مبادرة جديدة لتعزيز الشفافية في السوق السعودي، بما يخدم مستهدفات #رؤية_السعودية_2030 ويعزز بيئة المنافسة العادلة.", "https://twitter.com/GACOMPKSA/status/1", GacSocialMediaType.None, null, -1),
        new("tw-gac-2026-07-05-report", "صدر تقرير #الهيئة_العامة_للمنافسة عن الربع الثاني، ويكشف عن معالجة 68 طلب تركز اقتصادي بمتوسط 3.5 يوم لكل طلب — نقلة نوعية في الأداء التنظيمي.", "https://twitter.com/GACOMPKSA/status/2", GacSocialMediaType.None, null, -4),
        new("tw-gac-2026-07-02-oecd", "شاركت #الهيئة_العامة_للمنافسة في اجتماعات لجنة المنافسة بمنظمة #OECD، وقدمت تجربة المملكة الرائدة في الإصلاح التنظيمي لسوق المنافسة.", "https://twitter.com/GACOMPKSA/status/3", GacSocialMediaType.Image, "https://pbs.twimg.com/media/sample.jpg", -7),
        new("tw-gac-2026-06-28-workshop", "نظّمت #الهيئة_العامة_للمنافسة ورشة عمل تدريبية لأكثر من 120 مسؤولاً حكومياً حول تطبيق نظام المنافسة، تعزيزاً لثقافة الامتثال في القطاع العام.", "https://twitter.com/GACOMPKSA/status/4", GacSocialMediaType.None, null, -11),
        new("tw-gac-2026-06-25-decision", "أصدرت #الهيئة_العامة_للمنافسة قراراً بعدم الممانعة على صفقة تركز اقتصادي كبرى في قطاع التقنية بقيمة تتجاوز 8 مليارات ريال — دعماً للاستثمار وتحفيز الابتكار.", "https://twitter.com/GACOMPKSA/status/5", GacSocialMediaType.None, null, -14),
    };

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="SeedGacTwitterSamplesCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="dateTimeProvider">The injectable clock used to compute the relative sample timestamps.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public SeedGacTwitterSamplesCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider dateTimeProvider, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<SeedGacTwitterSamplesResult>> Handle(SeedGacTwitterSamplesCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        var inserted = 0;
        var skipped = 0;

        foreach (SampleSpec spec in Samples)
        {
            var exists = await _queryExecutor.AnyAsync(
                _dbContext.GacSocialPosts.Where(p => p.Platform == GacSocialPlatform.Twitter && p.ExternalId == spec.ExternalId),
                cancellationToken);
            if (exists)
            {
                skipped++;
                continue;
            }

            _dbContext.Add(spec.ToEntity(now));
            inserted++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "gac_social_post.seed_twitter",
            "GacSocialPost",
            "bulk",
            before: null,
            after: new { inserted, skipped },
            cancellationToken);

        return Result<SeedGacTwitterSamplesResult>.Success(new SeedGacTwitterSamplesResult(inserted, skipped, Samples.Count));
    }

    private sealed record SampleSpec(string ExternalId, string ContentAr, string PostUrl, GacSocialMediaType MediaType, string? MediaUrl, int DaysAgo)
    {
        public GacSocialPost ToEntity(DateTimeOffset now) => new()
        {
            Platform = GacSocialPlatform.Twitter,
            ExternalId = ExternalId,
            ContentAr = ContentAr,
            PostUrl = PostUrl,
            MediaType = MediaType,
            MediaUrl = MediaUrl,
            PostedAt = now.AddDays(DaysAgo),
            Account = Account,
        };
    }
}
