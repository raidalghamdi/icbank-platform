using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>One fixed demo news item template used by <see cref="SeedDemoNewsCommandHandler"/> (BUSINESS-RULES.md §5 seed-demo helper).</summary>
/// <param name="Kind">The item kind.</param>
/// <param name="Category">The item category.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="BodyAr">The Arabic body.</param>
/// <param name="SourceUrl">The source URL.</param>
/// <param name="DaysAgo">How many days before the seed clock this item is dated.</param>
/// <param name="ExternalRef">The optional external reference id.</param>
/// <param name="Tags">The searchable tag list.</param>
public sealed record DemoNewsFixture(
    GacNewsKind Kind, GacNewsCategory Category, string TitleAr, string BodyAr, string SourceUrl, int DaysAgo, string? ExternalRef, IReadOnlyList<string> Tags)
{
    /// <summary>Converts this fixture to a persistable <see cref="GacNewsItem"/> anchored at the given clock reading.</summary>
    /// <param name="now">The seed operation's current instant.</param>
    /// <returns>The mapped entity.</returns>
    public GacNewsItem ToEntity(DateTimeOffset now) => new()
    {
        Kind = Kind,
        Category = Category,
        TitleAr = TitleAr,
        BodyAr = BodyAr,
        SourceUrl = SourceUrl,
        PublishedAt = now.AddDays(-DaysAgo),
        ExternalRef = ExternalRef,
        Tags = Tags.ToList(),
    };
}
