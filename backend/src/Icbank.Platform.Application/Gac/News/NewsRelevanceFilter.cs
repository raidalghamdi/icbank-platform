namespace Icbank.Platform.Application.Gac.News;

/// <summary>
/// Decides whether a fetched article is genuinely about competition policy.
/// </summary>
/// <remarks>
/// Google News matches the configured Arabic search terms loosely, so a raw fetch drags in
/// sports fixtures, job listings and unrelated government authorities that merely share the
/// word "هيئة" or "منافسة". Without this filter roughly three quarters of a run is noise on
/// the media-monitoring page. Exclusions only apply when no unambiguous GAC signal is present,
/// so a genuine authority story that happens to mention an excluded word still gets through.
/// </remarks>
public static class NewsRelevanceFilter
{
    /// <summary>Vocabulary that marks an article as competition-related.</summary>
    private static readonly string[] RelevantTerms =
    {
        "هيئة المنافسة", "الهيئة العامة للمنافسة", "نظام المنافسة", "المنافسة العامة",
        "التركز الاقتصادي", "مكافحة الاحتكار", "احتكار", "الاحتكارية", "الممارسات المخلة",
        "اندماج", "الاندماج", "استحواذ", "الاستحواذ", "مكافحة الإغراق", "مكافحة إغراق",
        "المنافسة العادلة", "antitrust", "competition authority", "merger",
        "general authority for competition",
    };

    /// <summary>Known noise that survives the search terms.</summary>
    private static readonly string[] ExcludedTerms =
    {
        "المنافسات والمشتريات", "الهيئة العامة للنقل", "هيئة النقل", "وظائف", "جدارات",
        "الأمن العام", "في المرمى", "الدوري", "مباراة",
    };

    /// <summary>Signals strong enough to override <see cref="ExcludedTerms"/>.</summary>
    private static readonly string[] AuthorityTerms =
    {
        "هيئة المنافسة", "الهيئة العامة للمنافسة", "نظام المنافسة", "التركز الاقتصادي",
    };

    /// <summary>Determines whether an article should be stored.</summary>
    /// <param name="title">The article title.</param>
    /// <param name="body">The article body or summary, if any.</param>
    /// <returns><see langword="true"/> when the article is about competition policy.</returns>
    public static bool IsRelevant(string? title, string? body)
    {
        var text = (title ?? string.Empty) + " " + (body ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (ContainsAny(text, ExcludedTerms) && !ContainsAny(text, AuthorityTerms))
        {
            return false;
        }

        return ContainsAny(text, RelevantTerms);
    }

    private static bool ContainsAny(string text, string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
