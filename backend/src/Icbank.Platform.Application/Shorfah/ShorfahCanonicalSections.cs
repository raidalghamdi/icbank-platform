using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// The 18 canonical Shorfah paragraphs seeded on every new issue (BUSINESS-RULES.md §1.2). The
/// magazine's table of contents was restructured: the sections that used to be bundled inside
/// المرصد الاقتصادي and مؤشر النظام are now paragraphs in their own right, and each paragraph
/// carries the icon key the browser draws next to it. This exact list — order, Arabic titles,
/// Arabic definitions, display order and icon — is the fixed table of contents and must not be
/// reordered or reworded without a product decision.
/// </summary>
public static class ShorfahCanonicalSections
{
    /// <summary>The icon key returned for a section type the catalogue does not list.</summary>
    public const string FallbackIconKey = "newspaper";

    // Why: issues created before the restructure still carry section types that are no longer
    // seeded, and the browser still has to draw an icon for them.
    private static readonly Dictionary<ShorfahSectionType, string> LegacyIconKeys =
        new() { [ShorfahSectionType.GlobalNews] = "globe" };

    /// <summary>Gets the canonical section templates, in seed order.</summary>
    public static IReadOnlyList<ShorfahCanonicalSectionTemplate> Templates { get; } = new[]
    {
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.News,
            "أخبار المنافسة",
            "أبرز أخبار ومستجدات المنافسة خلال الشهر، وتشمل آخر التطورات والأخبار المتعلقة بالمنافسة بشكل عام، مثل القضايا الجديدة وأبرز المستجدات ذات العلاقة.",
            10,
            "newspaper"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.IntlParticipation,
            "مشاركاتنا الدولية",
            "أبرز مشاركات الهيئة في المؤتمرات والفعاليات والاجتماعات الدولية خلال الشهر، وأهم المشاركات الخارجية للهيئة.",
            20,
            "globe"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.OurComms,
            "تواصلنا",
            "تشمل أبرز أنشطة التواصل مع لقاءات القطاع الخاص وأوجه التواصل مع الجهات الحكومية.",
            30,
            "handshake"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.EconomicObservatory,
            "المرصد الاقتصادي",
            "أبرز الأرقام والمؤشرات الاقتصادية ذات العلاقة بالمنافسة خلال الشهر.",
            40,
            "chart"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.EconomicStudy,
            "دراسة اقتصادية",
            "أبرز الدراسات والتحليلات الاقتصادية ذات العلاقة بالمنافسة.",
            50,
            "research"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.CaseOfMonth,
            "قضية الشهر",
            "أبرز قضية أو موضوع مرتبط بالمنافسة خلال الشهر.",
            60,
            "folder"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.SystemIndex,
            "مؤشر النظام",
            "أبرز المؤشرات والبيانات المتعلقة بالنظام وأعمال الهيئة.",
            70,
            "gauge"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.CourtSessions,
            "جلسات قضائية",
            "أبرز الجلسات والأحكام والمستجدات القضائية ذات العلاقة بعمل الهيئة.",
            80,
            "gavel"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.MonopolyComplaints,
            "شكاوى وممارسات احتكارية",
            "أبرز الشكاوى والممارسات الاحتكارية التي تم رصدها أو التعامل معها خلال الشهر.",
            90,
            "alert"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.Settlements,
            "تسويات",
            "أبرز التسويات التي تمت خلال الشهر.",
            100,
            "check-circle"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.LegalWindow,
            "نافذة قانونية",
            "إطلالة مختصرة على أبرز التشريعات والأنظمة والتحديثات القانونية ذات العلاقة.",
            110,
            "scale"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.OfficeInterview,
            "حوار شهري",
            "حوار شهري مع أحد القيادات في الهيئة.",
            120,
            "mic"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.CompetitionCulture,
            "ثقافة المنافسة",
            "أبرز جهود ومبادرات نشر ثقافة المنافسة والتوعية بها.",
            130,
            "lightbulb"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.CompetitionInMonth,
            "المنافسة في شهر",
            "تلخيص لأبرز ما حدث في مجال المنافسة خلال الشهر، مع عرض أهم المستجدات والأنشطة والأحداث.",
            140,
            "calendar"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.OutsideBox,
            "خارج الصندوق",
            "مقال شهري يكتبه أحد موظفي الهيئة حول موضوع معرفي أو مهني أو إثرائي.",
            150,
            "sparkles"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.Events,
            "فعالياتنا",
            "أبرز فعاليات وأنشطة الهيئة التي تمت خلال الشهر.",
            160,
            "ticket"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.AgencyLit,
            "نشرتنا الهيئة",
            "التعريف بالموظفين الجدد في الهيئة خلال الشهر.",
            170,
            "user-plus"),
        new ShorfahCanonicalSectionTemplate(
            ShorfahSectionType.EmployeeQa,
            "أعطنا علومك",
            "مجموعة من الأسئلة الخفيفة والتعريفية التي تساعد الموظفين على التعرف على الموظف بشكل أفضل، من خلال التعرف على شخصيته واهتماماته وتجربته.",
            180,
            "quote"),
    };

    private static Dictionary<ShorfahSectionType, string> IconKeysByType { get; } =
        Templates.ToDictionary(template => template.SectionType, template => template.IconKey);

    /// <summary>Gets the icon key for a section type, falling back for types the catalogue no longer seeds.</summary>
    /// <param name="sectionType">The section type to resolve.</param>
    /// <returns>The catalogue's icon key, the legacy key for a dropped type, or <see cref="FallbackIconKey"/>.</returns>
    public static string IconKeyFor(ShorfahSectionType sectionType)
    {
        if (IconKeysByType.TryGetValue(sectionType, out var iconKey))
        {
            return iconKey;
        }

        return LegacyIconKeys.TryGetValue(sectionType, out var legacyIconKey) ? legacyIconKey : FallbackIconKey;
    }
}
