using System.Text;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Carries the "Week Start" generation prompt verbatim from BUSINESS-RULES.md §2.5
/// (<c>week-start.ts:365-381</c>), including the fixed length-option strings.
/// </summary>
public static class WeekStartPromptTemplate
{
    /// <summary>The Node source's default tone when none is supplied.</summary>
    public const string DefaultTone = "ودية";

    private const string ShortLength = "قصير (سطران فقط - 25-40 كلمة)";
    private const string MediumLength = "متوسط (3 أسطر - 45-65 كلمة)";
    private const string LongLength = "طويل (4 أسطر - 70-90 كلمة)";

    /// <summary>Resolves the fixed Arabic length descriptor for a length key, defaulting to medium exactly like the Node source.</summary>
    /// <param name="length">The caller-supplied length key: <c>short</c>, <c>medium</c>, or <c>long</c>.</param>
    /// <returns>The verbatim Arabic length descriptor string.</returns>
    public static string ResolveLengthText(string? length) => length switch
    {
        "short" => ShortLength,
        "long" => LongLength,
        _ => MediumLength,
    };

    /// <summary>Builds the full prompt text for one generation request.</summary>
    /// <param name="styleInfo">The derived style-profile digest text.</param>
    /// <param name="archiveContext">The optional top-5 archive-similarity context, or <c>null</c>/empty when there is none.</param>
    /// <param name="topic">The week's topic.</param>
    /// <param name="occasion">The optional occasion.</param>
    /// <param name="audience">The optional audience.</param>
    /// <param name="tone">The optional tone; defaults to <see cref="DefaultTone"/>.</param>
    /// <param name="lengthText">The resolved length descriptor from <see cref="ResolveLengthText"/>.</param>
    /// <returns>The fully-assembled prompt text.</returns>
    public static string Build(string styleInfo, string? archiveContext, string topic, string? occasion, string? audience, string? tone, string lengthText)
    {
        var builder = new StringBuilder();
        builder.Append("أنت كاتب محتوى داخلي لجهة حكومية سعودية.\n");
        builder.Append("اكتب رسالة \"بداية أسبوع\" قصيرة جداً بالعربية الفصحى المبسطة، ملتزماً بهذا الأسلوب:\n");
        builder.Append(styleInfo).Append('\n').Append('\n');

        if (!string.IsNullOrWhiteSpace(archiveContext))
        {
            builder.Append("مستفيداً من النماذج السابقة:\n").Append(archiveContext).Append("\n\n");
        }

        builder.Append("موضوع هذا الأسبوع: ").Append(topic).Append('\n');
        if (!string.IsNullOrWhiteSpace(occasion))
        {
            builder.Append("المناسبة: ").Append(occasion).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(audience))
        {
            builder.Append("الجمهور: ").Append(audience).Append('\n');
        }

        builder.Append("النبرة: ").Append(string.IsNullOrWhiteSpace(tone) ? DefaultTone : tone).Append('\n');
        builder.Append("الطول: ").Append(lengthText).Append("\n\n");
        builder.Append("⚠️ تعليمات حرجة:\n");
        builder.Append("- التزم بعدد الأسطر المحدد بدقة (2-4 أسطر فقط حسب الخيار)\n");
        builder.Append("- رسائل بداية الأسبوع الفعالة قصيرة ومركّزة - لا فقرات طويلة\n");
        builder.Append("- جملة افتتاحية حانية + فكرة رئيسية واحدة + دعوة للعمل\n");
        builder.Append("- بدون مقدمات أو شروحات\n\n");
        builder.Append("أخرج النص جاهزاً للنشر.");
        return builder.ToString();
    }
}
