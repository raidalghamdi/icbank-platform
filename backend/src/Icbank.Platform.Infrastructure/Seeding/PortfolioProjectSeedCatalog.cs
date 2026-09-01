using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The starter portfolio the projects page ships with: three operational projects and two
/// strategic ones. These are illustrative rows, not authority records, so the page has something
/// truthful in shape to track until real projects are entered — they carry no figures anybody
/// should quote.
/// </summary>
internal static class PortfolioProjectSeedCatalog
{
    /// <summary>Gets the seeded projects, keyed by <see cref="PortfolioProjectSeedRow.Code"/>.</summary>
    internal static IReadOnlyList<PortfolioProjectSeedRow> Rows { get; } = new[]
    {
        new PortfolioProjectSeedRow(
            "OPS-01",
            "تشغيل مركز الرصد الإعلامي",
            "رصد يومي للتغطية الإعلامية وإصدار التقارير الدورية للإدارة العليا.",
            ProjectCategory.Operational,
            ProjectStage.InProgress,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            74,
            6,
            -120,
            60,
            "اكتمل ربط مصادر الرصد الآلي، والعمل جارٍ على أتمتة التقرير الأسبوعي.",
            1,
            new[]
            {
                new PortfolioMilestoneSeedRow("تحديد مصادر الرصد المعتمدة", -95, true),
                new PortfolioMilestoneSeedRow("تشغيل الجلب الآلي للأخبار", -40, true),
                new PortfolioMilestoneSeedRow("أتمتة التقرير الأسبوعي", 20, false),
                new PortfolioMilestoneSeedRow("تسليم لوحة مؤشرات الرصد", 55, false),
            }),
        new PortfolioProjectSeedRow(
            "OPS-02",
            "إصدار نشرة شُرفة الداخلية",
            "إعداد النشرة الداخلية الشهرية وتنسيق مساهمات الإدارات ومراجعتها قبل النشر.",
            ProjectCategory.Operational,
            ProjectStage.InProgress,
            "لمى العتيبي",
            "إدارة الاتصال الداخلي",
            58,
            4,
            -75,
            35,
            "أُغلقت مساهمات الإدارات للعدد الحالي، وبقيت المراجعة اللغوية والإخراج.",
            2,
            new[]
            {
                new PortfolioMilestoneSeedRow("اعتماد خطة الأعداد السنوية", -70, true),
                new PortfolioMilestoneSeedRow("جمع مساهمات الإدارات", -15, true),
                new PortfolioMilestoneSeedRow("المراجعة اللغوية والإخراج", 12, false),
                new PortfolioMilestoneSeedRow("نشر العدد وتوزيعه", 30, false),
            }),
        new PortfolioProjectSeedRow(
            "OPS-03",
            "تحديث الهوية البصرية للمنصات الرقمية",
            "توحيد القوالب والخطوط والألوان عبر القنوات الرقمية للهيئة.",
            ProjectCategory.Operational,
            ProjectStage.OnHold,
            "عبدالرحمن الشهري",
            "إدارة الهوية والتصميم",
            31,
            5,
            -60,
            75,
            "العمل متوقف مؤقتاً بانتظار اعتماد ترخيص الخط الرسمي.",
            3,
            new[]
            {
                new PortfolioMilestoneSeedRow("جرد القوالب الحالية", -45, true),
                new PortfolioMilestoneSeedRow("اعتماد دليل الهوية المحدَّث", 15, false),
                new PortfolioMilestoneSeedRow("تطبيق القوالب على القنوات", 70, false),
            }),
        new PortfolioProjectSeedRow(
            "STR-01",
            "منصة تواصُلنا — التحول الرقمي للتواصل المؤسسي",
            "بناء منصة موحّدة تجمع الرصد والنشرات والتصاميم وتقارير الأداء في مكان واحد.",
            ProjectCategory.Strategic,
            ProjectStage.InProgress,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            47,
            9,
            -150,
            140,
            "اكتملت وحدات الرصد والتصاميم، والعمل جارٍ على وحدة تتبع المشاريع.",
            1,
            new[]
            {
                new PortfolioMilestoneSeedRow("اعتماد معمارية المنصة", -130, true),
                new PortfolioMilestoneSeedRow("إطلاق وحدة الرصد الإعلامي", -35, true),
                new PortfolioMilestoneSeedRow("إطلاق وحدة تتبع المشاريع", 25, false),
                new PortfolioMilestoneSeedRow("التشغيل الكامل والتسليم", 130, false),
            }),
        new PortfolioProjectSeedRow(
            "STR-02",
            "برنامج عام الذكاء الاصطناعي 2026",
            "تفعيل مبادرات الهيئة ضمن عام الذكاء الاصطناعي وقياس أثرها الإعلامي.",
            ProjectCategory.Strategic,
            ProjectStage.InProgress,
            "نورة القحطاني",
            "إدارة المبادرات",
            62,
            7,
            -200,
            95,
            "أُنجزت ثلاث مبادرات من أصل خمس، والمتبقي مرتبط بشراكات خارجية.",
            2,
            new[]
            {
                new PortfolioMilestoneSeedRow("إطلاق الحملة التعريفية", -170, true),
                new PortfolioMilestoneSeedRow("ورش العمل الداخلية", -60, true),
                new PortfolioMilestoneSeedRow("الشراكات مع الجهات المعنية", 40, false),
                new PortfolioMilestoneSeedRow("تقرير الأثر الختامي", 90, false),
            }),
    };
}
