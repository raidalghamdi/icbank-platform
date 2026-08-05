using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>The fixed 6-item demo news fixture set (BUSINESS-RULES.md §5 seed-demo helper), ported verbatim from the Node source.</summary>
public static class DemoNewsFixtures
{
    /// <summary>Gets the fixed demo news fixtures, in seed order.</summary>
    public static IReadOnlyList<DemoNewsFixture> All { get; } = new[]
    {
        new DemoNewsFixture(
            GacNewsKind.Decision,
            GacNewsCategory.MergerApproval,
            "الهيئة العامة للمنافسة توافق على 22 طلب تركز اقتصادي خلال يوليو",
            "أعلنت الهيئة العامة للمنافسة عن إصدار 22 قراراً بعدم الممانعة على طلبات التركز الاقتصادي خلال الأسبوع الأول من يوليو 2026، في قطاعات التجزئة والتقنية والخدمات اللوجستية.",
            "https://www.spa.gov.sa/example-1",
            1,
            "GAC-DEC-2026-107",
            new[] { "تركز", "ترخيص", "قرارات" }),
        new DemoNewsFixture(
            GacNewsKind.News,
            GacNewsCategory.Awareness,
            "منتدى المنافسة العادلة يناقش أفضل الممارسات الدولية",
            "استضافت الهيئة منتدى المنافسة العادلة 2026 بمشاركة ممثلي هيئات دولية من الاتحاد الأوروبي ومنطقة الشرق الأوسط، لمناقشة تأثير الذكاء الاصطناعي على الأسواق الرقمية.",
            "https://gac.gov.sa/news-forum",
            2,
            null,
            new[] { "منتدى", "دولي", "توعية" }),
        new DemoNewsFixture(
            GacNewsKind.Decision,
            GacNewsCategory.Enforcement,
            "تحقيقات جديدة في مخالفات تملّك محتمل في قطاع المواد الغذائية",
            "فتحت الهيئة ملفات تحقيق في ممارسات تقييدية محتملة من قبل 3 منشآت في قطاع توزيع المواد الغذائية في ثلاث مناطق رئيسية.",
            "https://gac.gov.sa/enforcement-2026-07",
            3,
            "GAC-ENF-2026-041",
            new[] { "تحقيق", "غذاء", "إنفاذ" }),
        new DemoNewsFixture(
            GacNewsKind.News,
            GacNewsCategory.Awareness,
            "دورة تدريبية تخصصية لمحققي المنافسة في جدة",
            "إختتمت الهيئة دورة تدريبية مكثفة لـ 45 محقق منافسة حول تحليل الأسواق الرقمية وتحديد الممارسات المخالفة.",
            "https://gac.gov.sa/training-jeddah",
            4,
            null,
            new[] { "تدريب", "بناء قدرات" }),
        new DemoNewsFixture(
            GacNewsKind.Decision,
            GacNewsCategory.MergerConditional,
            "الموافقة المشروطة على صفقة استحواذ في قطاع التأمين الصحي",
            "وافقت الهيئة مشروطًا على طلب تركز لشركات تأمين صحي مع التزامات محددة لحماية حقوق المستفيدين.",
            "https://spa.gov.sa/example-5",
            5,
            "GAC-DEC-2026-108",
            new[] { "تأمين", "استحواذ", "مشروط" }),
        new DemoNewsFixture(
            GacNewsKind.News,
            GacNewsCategory.Awareness,
            "تقرير سنوي: مؤشر المنافسة في المملكة يرتفع إلى 84%",
            "حقّقت المملكة تقدماً لافتاً في مؤشر المنافسة العالمي خلال 2026 وفقاً للتقرير السنوي، بزيادة 6 نقاط عن العام السابق.",
            "https://gac.gov.sa/annual-report-2026",
            6,
            null,
            new[] { "مؤشر", "تقدم", "تقرير سنوي" }),
    };
}
