using System.Globalization;
using System.Text;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>
/// Assembles the final icon-event extraction prompt from the verbatim static segments in
/// <see cref="IconEventExtractionPrompts"/> plus the caller's actual data, reproducing the Node
/// source's 3 template-literal interpolation points exactly (BUSINESS-RULES.md §7.4).
/// </summary>
public static class IconEventPromptBuilder
{
    /// <summary>Builds the full prompt text for one generation request.</summary>
    /// <param name="rawData">The raw free-text event data, if supplied.</param>
    /// <param name="headline">The explicit headline, used to build the structured fallback block when <paramref name="rawData"/> is absent.</param>
    /// <param name="subtitle">The explicit subtitle.</param>
    /// <param name="department">The explicit, confirmed department name.</param>
    /// <param name="hashtag">The explicit, confirmed hashtag.</param>
    /// <param name="date">The explicit event date.</param>
    /// <param name="time">The explicit event time.</param>
    /// <param name="location">The explicit event location.</param>
    /// <param name="eventType">The explicit event type.</param>
    /// <returns>The fully-assembled prompt text.</returns>
    public static string Build(
        string? rawData,
        string? headline,
        string? subtitle,
        string? department,
        string? hashtag,
        string? date,
        string? time,
        string? location,
        string? eventType)
    {
        var dataBlock = string.IsNullOrWhiteSpace(rawData)
            ? BuildStructuredFallback(headline, subtitle, department, date, time, location, eventType)
            : rawData;

        var builder = new StringBuilder();
        builder.Append(IconEventExtractionPrompts.Seg1).Append(dataBlock).Append(IconEventExtractionPrompts.Seg2);
        builder.Append(BuildConfirmationNote(department, hashtag));
        builder.Append(IconEventExtractionPrompts.Seg3).Append(BuildIconListForAi());
        builder.Append(IconEventExtractionPrompts.Seg4);
        return builder.ToString();
    }

    private static string BuildStructuredFallback(
        string? headline, string? subtitle, string? department, string? date, string? time, string? location, string? eventType)
    {
        const string dash = "-";
        return string.Create(CultureInfo.InvariantCulture, $"العنوان: {headline}\nالوصف: {subtitle ?? dash}\nالإدارة: {department ?? dash}\nالتاريخ: {date ?? dash}\nالوقت: {time ?? dash}\nالمكان: {location ?? dash}\nالنوع: {eventType ?? dash}");
    }

    private static string BuildConfirmationNote(string? department, string? hashtag)
    {
        var departmentNote = string.IsNullOrWhiteSpace(department)
            ? "⚠️ لم يُذكر اسم إدارة — اتركه سلسلة فارغة \"\" (لا تخترع)."
            : string.Create(CultureInfo.InvariantCulture, $"ملاحظة: اسم الإدارة المؤكد هو \"{department}\" — استخدمه كما هو (بدون اختصار في هذا الحقل).");
        var hashtagNote = string.IsNullOrWhiteSpace(hashtag)
            ? "⚠️ لم يُذكر هاشتاج — اتركه سلسلة فارغة \"\" (لا تخترع هاشتاقاً)."
            : string.Create(CultureInfo.InvariantCulture, $"ملاحظة: الهاشتاج المؤكد هو \"{hashtag}\" — استخدمه كما هو.");
        return string.Create(CultureInfo.InvariantCulture, $"{departmentNote}\n{hashtagNote}");
    }

    private static string BuildIconListForAi()
    {
        var builder = new StringBuilder();
        foreach (IconDefinition icon in IconLibrary.All)
        {
            builder.Append("- ").Append(icon.Name).Append(" (").Append(icon.LabelAr).Append(")\n");
        }

        return builder.ToString();
    }
}
