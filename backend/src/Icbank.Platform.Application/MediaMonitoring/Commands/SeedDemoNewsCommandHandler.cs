using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="SeedDemoNewsCommand"/>. Ports the Node source's fixed 6-news/6-post demo fixture set verbatim (BUSINESS-RULES.md §5, demo-seed helper).</summary>
public sealed class SeedDemoNewsCommandHandler : IRequestHandler<SeedDemoNewsCommand, Result<SeedDemoNewsResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="SeedDemoNewsCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="dateTimeProvider">The Riyadh-aware clock port.</param>
    public SeedDemoNewsCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<SeedDemoNewsResultDto>> Handle(SeedDemoNewsCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _dateTimeProvider.RiyadhNow;
        var news = DemoNewsFixtures.All.Select(fixture => fixture.ToEntity(now)).ToList();
        var posts = DemoSocialPostFixtures.All.Select(fixture => fixture.ToEntity(now)).ToList();

        foreach (GacNewsItem item in news)
        {
            _dbContext.Add(item);
        }

        foreach (GacSocialPost post in posts)
        {
            _dbContext.Add(post);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.seed_demo",
            "GacNewsItem",
            "seed-demo",
            before: null,
            after: new { SeededNews = news.Count, SeededPosts = posts.Count },
            cancellationToken);

        var message = $"تم زراعة {news.Count} خبر و {posts.Count} منشور تجريبي حديث.";
        return Result<SeedDemoNewsResultDto>.Success(new SeedDemoNewsResultDto(message, news.Count, posts.Count));
    }
}
