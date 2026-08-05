using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// The 13 canonical Shorfah section types seeded on every new issue (BUSINESS-RULES.md §1.2),
/// ported verbatim (order, Arabic titles/descriptions, display order) from
/// <c>SHORFAH_DEFAULT_SECTIONS</c> in <c>shorfah.ts:122-141</c>. This exact list is the
/// magazine's fixed table of contents and must not be reordered or reworded without a product
/// decision.
/// </summary>
public static class ShorfahCanonicalSections
{
    /// <summary>Gets the canonical section templates, in seed order.</summary>
    public static IReadOnlyList<ShorfahCanonicalSectionTemplate> Templates { get; } = new[]
    {
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.GlobalNews, "أخبار دولية", "أبرز الأخبار الدولية ذات الصلة", 10),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.News, "أخبارنا", "أبرز أخبار الهيئة هذا الشهر", 20),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.IntlParticipation, "مشاركاتنا الدولية", "مشاركات الهيئة في المحافل والفعاليات الدولية", 30),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.OurComms, "تواصلنا", "يشمل لقاءات القطاع الخاص وجهود التواصل مع الجهات الحكومية", 40),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.EconomicObservatory, "المرصد الاقتصادي", "أرقام اقتصادية، تركيز الشهر، دراسة اقتصادية، قضية الشهر", 50),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.SystemIndex, "مؤشر النظام", "الجلسات القضائية، الشكاوى، الممارسات، التسويات", 60),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.LegalWindow, "نافذة قانونية", "إطلالة على التشريعات والأنظمة", 70),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.OfficeInterview, "في مكتبهم", "حوار شهري مع أحد القياديين", 80),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.CompetitionCulture, "ثقافة المنافسة", "جهود نشر ثقافة المنافسة + المنافسة في شهر", 90),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.OutsideBox, "خارج الصندوق", "مقال شهري من موظف", 100),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.Events, "فعالياتنا", "فعاليات الهيئة", 110),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.AgencyLit, "نورتنا الهيئة", "إنجازات أفراد الهيئة داخل وخارج مقرها", 120),
        new ShorfahCanonicalSectionTemplate(ShorfahSectionType.EmployeeQa, "عطنا علومك", "حوار مع موظف", 130),
    };
}
