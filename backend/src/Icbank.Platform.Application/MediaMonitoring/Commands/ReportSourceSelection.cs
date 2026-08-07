using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Interprets the report generator's source-channel checkboxes.
/// </summary>
/// <remarks>
/// Exists because the UI's three checkboxes (news / LinkedIn / Twitter) do not map one-to-one onto
/// <see cref="GacSocialPlatform"/>, which also has Instagram and YouTube. Rather than silently
/// dropping Instagram and YouTube posts the moment any filter is applied, they are treated as
/// unnamed social channels and included whenever at least one social channel is selected. An unknown
/// or misspelled key is ignored rather than rejected, so a future front-end addition cannot break
/// report generation before the backend catches up.
/// </remarks>
public sealed class ReportSourceSelection
{
    /// <summary>The checkbox key selecting press/news coverage.</summary>
    public const string NewsKey = "news";

    /// <summary>The checkbox key selecting LinkedIn posts.</summary>
    public const string LinkedInKey = "linkedin";

    /// <summary>The checkbox key selecting Twitter/X posts.</summary>
    public const string TwitterKey = "twitter";

    private static readonly GacSocialPlatform[] UnnamedSocialPlatforms =
    {
        GacSocialPlatform.Instagram,
        GacSocialPlatform.YouTube,
    };

    private readonly HashSet<GacSocialPlatform> _platforms;

    private ReportSourceSelection(bool includeNews, HashSet<GacSocialPlatform> platforms)
    {
        IncludeNews = includeNews;
        _platforms = platforms;
    }

    /// <summary>Gets a value indicating whether press/news items should be included.</summary>
    public bool IncludeNews { get; }

    /// <summary>Gets a value indicating whether any social platform is selected.</summary>
    public bool IncludeAnySocial => _platforms.Count > 0;

    /// <summary>Gets the selected social platforms.</summary>
    public IReadOnlyCollection<GacSocialPlatform> Platforms => _platforms;

    /// <summary>Builds a selection from the raw checkbox keys.</summary>
    /// <param name="sources">The requested keys, or null/empty to select everything.</param>
    /// <returns>The resolved selection.</returns>
    public static ReportSourceSelection From(IReadOnlyList<string>? sources)
    {
        if (sources is null || sources.Count == 0)
        {
            return All();
        }

        var keys = new HashSet<string>(
            sources.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
            StringComparer.OrdinalIgnoreCase);

        if (keys.Count == 0)
        {
            return All();
        }

        var platforms = new HashSet<GacSocialPlatform>();
        if (keys.Contains(LinkedInKey))
        {
            platforms.Add(GacSocialPlatform.LinkedIn);
        }

        if (keys.Contains(TwitterKey))
        {
            platforms.Add(GacSocialPlatform.Twitter);
        }

        if (platforms.Count > 0)
        {
            foreach (GacSocialPlatform platform in UnnamedSocialPlatforms)
            {
                platforms.Add(platform);
            }
        }

        return new ReportSourceSelection(keys.Contains(NewsKey), platforms);
    }

    /// <summary>Builds the include-everything selection.</summary>
    /// <returns>A selection covering news and every social platform.</returns>
    private static ReportSourceSelection All() =>
        new(includeNews: true, new HashSet<GacSocialPlatform>(Enum.GetValues<GacSocialPlatform>()));
}
