using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// The 3 audience-tiered media-report prompt templates (BUSINESS-RULES.md §5.1, verbatim from
/// <c>getAudiencePrompt(audience)</c> in <c>media-monitoring.ts:53-118</c>). Carried over exactly
/// as product IP. <see cref="MediaReportAudience.Manager"/> is both the default and the fallback
/// for any unrecognized audience value, matching the Node source.
/// </summary>
public static class AudienceReportPromptTemplates
{
    private const string ExecutivePrompt =
        "أنت محلل إعلامي تنفيذي. ولّد ملخصاً تنفيذياً موجزاً للقيادة العليا بصيغة Markdown عربية:\n\n" +
        "## الملخص التنفيذي\n" +
        "3 نقاط رئيسية فقط (سطر واحد لكل نقطة)\n\n" +
        "## أبرز رسالة\n" +
        "رسالة مؤسسية واحدة بارزة في الفترة\n\n" +
        "## نبرة الفترة\n" +
        "سطر واحد يصف النبرة العامة\n\n" +
        "## التوصية التنفيذية\n" +
        "توصية واحدة قابلة للتنفيذ\n\n" +
        "استخدم لغة موجزة احترافية. لا تكرر النصوص الخام.";

    private const string AnalystPrompt =
        "أنت محلل إعلامي خبير. ولّد تقريراً تفصيلياً شاملاً بصيغة Markdown عربية:\n\n" +
        "## 1. ملخص الفترة\n" +
        "فقرة (4-6 أسطر) تلخّص النشاط\n\n" +
        "## 2. تحليل كل منشور\n" +
        "لكل منشور: التاريخ، الموضوع، نبرة الصوت (تنظيمية/ترويجية/توعوية/اجتماعية/دينية)، الهدف المحتمل، التأثير المتوقع\n\n" +
        "## 3. الموضوعات السائدة\n" +
        "قائمة بأبرز 5-7 موضوعات مع نسبة التكرار\n\n" +
        "## 4. تحليل النبرة الإجمالي\n" +
        "توزيع نسبي بين الأنواع المختلفة + ملاحظات نوعية\n\n" +
        "## 5. الفجوات والفرص\n" +
        "- ما المحاور الناقصة؟\n" +
        "- ما الفرص للمحتوى المستقبلي؟\n\n" +
        "## 6. التوصيات\n" +
        "5 توصيات عملية مرتبة بالأولوية\n\n" +
        "استخدم لغة دقيقة محايدة. أرفق اقتباسات وأرقاماً حيثما أمكن.";

    private const string ManagerPrompt =
        "أنت محلل إعلامي محترف. ولّد تقرير رصد متوازناً للإدارة الوسطى بصيغة Markdown عربية:\n\n" +
        "## ملخص الفترة\n" +
        "فقرة قصيرة (3 أسطر)\n\n" +
        "## أبرز المنشورات والأخبار\n" +
        "جدول بأهم 5-7 عناصر: التاريخ | الموضوع | النبرة | الرابط\n\n" +
        "## تحليل النبرة\n" +
        "- التوزيع بين الأنواع (تنظيمي/ترويجي/توعوي/اجتماعي/...)\n" +
        "- ملاحظات على الاتجاه العام\n\n" +
        "## الموضوعات الرئيسية\n" +
        "قائمة بـ 3-5 موضوعات مع شرح موجز\n\n" +
        "## توصيات\n" +
        "3-4 توصيات عملية\n\n" +
        "استخدم لغة احترافية واضحة. أرفق روابط المنشورات.";

    /// <summary>Resolves the audience-tiered prompt text for the given audience key.</summary>
    /// <param name="audience">The parsed audience tier.</param>
    /// <returns>The verbatim Arabic prompt template for that tier.</returns>
    public static string Resolve(MediaReportAudience audience) => audience switch
    {
        MediaReportAudience.Executive => ExecutivePrompt,
        MediaReportAudience.Analyst => AnalystPrompt,
        _ => ManagerPrompt,
    };
}
