namespace Icbank.Platform.Infrastructure.News;

/// <summary>
/// Configuration for the news ingest pipeline, bound from the <c>NewsSources</c> section.
/// </summary>
/// <remarks>
/// Search terms live here rather than in code so the Authority's monitoring team can retune what is
/// tracked through an App Service setting without waiting for a deployment.
/// </remarks>
public sealed class NewsSourceOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "NewsSources";

    /// <summary>Gets or sets the provider keys to query, in order.</summary>
    /// <remarks>
    /// Defaults to Google News only. NewsData.io is registered but left out of this list because an
    /// August 2026 coverage test returned zero results for the Authority and its Saudi source list
    /// omits أرقام, صحيفة مال, الاقتصادية, سبق and أخبار 24. Add <c>newsdata-io</c> here (with a key
    /// set) to re-enable it without a code change.
    /// </remarks>
    public List<string> EnabledProviders { get; set; } = new() { GoogleNewsRssProvider.Key };

    /// <summary>Gets or sets the search terms to track.</summary>
    public List<string> Terms { get; set; } = new()
    {
        "هيئة المنافسة العامة",
        "الهيئة العامة للمنافسة",
        "نظام المنافسة السعودي",
        "التركز الاقتصادي السعودية",
    };

    /// <summary>Gets or sets the language to restrict results to.</summary>
    public string Language { get; set; } = "ar";

    /// <summary>Gets or sets the region to bias results toward.</summary>
    public string Region { get; set; } = "SA";

    /// <summary>Gets or sets how many days back each scheduled fetch looks.</summary>
    public int WithinDays { get; set; } = 7;

    /// <summary>Gets or sets the per-term cap on retrieved items.</summary>
    public int MaxItemsPerTerm { get; set; } = 50;

    /// <summary>Gets or sets the base URL for Google News RSS search.</summary>
    public string GoogleNewsBaseUrl { get; set; } = "https://news.google.com/rss/search";

    /// <summary>Gets or sets the base URL for the NewsData.io latest-news endpoint.</summary>
    public string NewsDataBaseUrl { get; set; } = "https://newsdata.io/api/1/latest";
}
