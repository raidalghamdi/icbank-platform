using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The department's tracked portfolio as the authority currently runs it: three operational
/// projects and one strategic programme. This catalogue is the source of truth the seeder
/// reconciles the <c>portfolio_projects</c> table against — a code missing from here is a project
/// the department no longer tracks, so it is removed rather than left behind on the page.
/// </summary>
internal static class PortfolioProjectSeedCatalog
{
    /// <summary>Gets the seeded projects, keyed by <see cref="PortfolioProjectSeedRow.Code"/>.</summary>
    internal static IReadOnlyList<PortfolioProjectSeedRow> Rows { get; } = new[]
    {
        new PortfolioProjectSeedRow(
            "OPS-01",
            "تشغيل مركز الاتصال الموحد للهيئة العامة للمنافسة للعام 2023م",
            "تشغيل مركز الاتصال الموحد واستقبال بلاغات واستفسارات المستفيدين وقياس مستوى الخدمة.",
            ProjectCategory.Operational,
            ProjectStage.InProgress,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            68,
            12,
            -210,
            90,
            "اكتمل تشغيل الوردية المسائية، والعمل جارٍ على رفع نسبة الرد خلال الدقيقة الأولى.",
            1,
            new[]
            {
                new PortfolioMilestoneSeedRow("تجهيز البنية التقنية للمركز", -185, true),
                new PortfolioMilestoneSeedRow("تدريب فريق الاتصال وتشغيل الوردية الصباحية", -120, true),
                new PortfolioMilestoneSeedRow("تشغيل الوردية المسائية", -30, true),
                new PortfolioMilestoneSeedRow("اعتماد تقرير مستوى الخدمة السنوي", 75, false),
            }),
        new PortfolioProjectSeedRow(
            "OPS-02",
            "إعداد التقرير السنوي 2025",
            "جمع منجزات الإدارات وإعداد التقرير السنوي للهيئة وتصميمه واعتماده قبل النشر.",
            ProjectCategory.Operational,
            ProjectStage.InProgress,
            "لمى العتيبي",
            "إدارة الاتصال الداخلي",
            45,
            6,
            -95,
            110,
            "اكتمل جمع مساهمات الإدارات، والعمل جارٍ على الصياغة التحريرية للفصل الأول.",
            2,
            new[]
            {
                new PortfolioMilestoneSeedRow("اعتماد هيكل التقرير ومحاوره", -80, true),
                new PortfolioMilestoneSeedRow("جمع منجزات الإدارات وبياناتها", -20, true),
                new PortfolioMilestoneSeedRow("الصياغة التحريرية والمراجعة اللغوية", 45, false),
                new PortfolioMilestoneSeedRow("التصميم والإخراج النهائي والاعتماد", 100, false),
            }),
        new PortfolioProjectSeedRow(
            "OPS-03",
            "تقديم خدمات الترجمة للهيئة العامة للمنافسة",
            "ترجمة الوثائق والتقارير والمحتوى الإعلامي للهيئة وضبط جودتها ضمن اتفاقية مستوى الخدمة.",
            ProjectCategory.Operational,
            ProjectStage.InProgress,
            "عبدالرحمن الشهري",
            "إدارة المحتوى والنشر",
            52,
            4,
            -150,
            200,
            "أُنجزت دفعة الترجمة الثالثة في وقتها، والعمل جارٍ على توحيد المصطلحات المعتمدة.",
            3,
            new[]
            {
                new PortfolioMilestoneSeedRow("اعتماد اتفاقية مستوى الخدمة مع مزود الترجمة", -140, true),
                new PortfolioMilestoneSeedRow("تسليم دفعة الترجمة الأولى", -95, true),
                new PortfolioMilestoneSeedRow("بناء المسرد الموحد للمصطلحات", 60, false),
                new PortfolioMilestoneSeedRow("تقرير جودة الترجمة الختامي", 190, false),
            }),
        new PortfolioProjectSeedRow(
            "STR-01",
            "مشروع حملة التوعية بالاستراتيجية وتعزيز القيم",
            "حملة داخلية للتعريف باستراتيجية الهيئة وترسيخ قيمها لدى الموظفين وقياس أثرها.",
            ProjectCategory.Strategic,
            ProjectStage.InProgress,
            "نورة القحطاني",
            "إدارة المبادرات",
            38,
            8,
            -70,
            160,
            "اعتُمدت الهوية الإبداعية للحملة، والعمل جارٍ على إنتاج المواد التوعوية للمرحلة الأولى.",
            1,
            new[]
            {
                new PortfolioMilestoneSeedRow("دراسة قياس الوعي المبدئي", -55, true),
                new PortfolioMilestoneSeedRow("اعتماد الهوية الإبداعية للحملة", -10, true),
                new PortfolioMilestoneSeedRow("إطلاق المرحلة الأولى من الحملة", 50, false),
                new PortfolioMilestoneSeedRow("قياس أثر الحملة وتقرير النتائج", 150, false),
            }),
    };
}
