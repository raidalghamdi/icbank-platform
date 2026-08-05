using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>One fixed demo social-post template used by <see cref="SeedDemoNewsCommandHandler"/> (BUSINESS-RULES.md §5 seed-demo helper).</summary>
/// <param name="Platform">The source platform.</param>
/// <param name="ExternalIdSuffix">The fixture-specific suffix appended to a timestamp to build a unique external id.</param>
/// <param name="ContentAr">The Arabic post content.</param>
/// <param name="PostUrl">The original post URL.</param>
/// <param name="DaysAgo">How many days before the seed clock this post is dated.</param>
/// <param name="Likes">The like count.</param>
/// <param name="Comments">The comment count.</param>
/// <param name="Shares">The share count.</param>
public sealed record DemoSocialPostFixture(
    GacSocialPlatform Platform, string ExternalIdSuffix, string ContentAr, string PostUrl, int DaysAgo, int Likes, int Comments, int Shares)
{
    private const string SeedAccount = "SaudiGAC";

    /// <summary>Converts this fixture to a persistable <see cref="GacSocialPost"/> anchored at the given clock reading.</summary>
    /// <param name="now">The seed operation's current instant.</param>
    /// <returns>The mapped entity.</returns>
    public GacSocialPost ToEntity(DateTimeOffset now) => new()
    {
        Platform = Platform,
        ExternalId = $"demo-{ExternalIdSuffix}-{now.ToUnixTimeMilliseconds()}",
        ContentAr = ContentAr,
        PostUrl = PostUrl,
        PostedAt = now.AddDays(-DaysAgo),
        Account = SeedAccount,
        Metrics = new SocialMetrics { Likes = Likes, Comments = Comments, Shares = Shares },
    };
}
