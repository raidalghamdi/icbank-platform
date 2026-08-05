using Icbank.Platform.Domain.Gac;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>The fixed 6-item demo social-post fixture set (BUSINESS-RULES.md §5 seed-demo helper), ported verbatim from the Node source.</summary>
public static class DemoSocialPostFixtures
{
    /// <summary>Gets the fixed demo social-post fixtures, in seed order.</summary>
    public static IReadOnlyList<DemoSocialPostFixture> All { get; } = new[]
    {
        new DemoSocialPostFixture(
            GacSocialPlatform.LinkedIn,
            "li-1",
            "أعلنت #الهيئة_العامة_للمنافسة إصدار 22 قراراً بعدم الممانعة في يوليو، مما يعزز دورها في حماية المنافسة العادلة.",
            "https://linkedin.com/posts/gac-demo-1",
            1,
            342,
            18,
            47),
        new DemoSocialPostFixture(
            GacSocialPlatform.LinkedIn,
            "li-2",
            "خلال منتدى المنافسة العادلة 2026، أكد معالي الرئيس أهمية التعاون الدولي لمواجهة تحديات الأسواق الرقمية والذكاء الاصطناعي.",
            "https://linkedin.com/posts/gac-demo-2",
            2,
            511,
            34,
            89),
        new DemoSocialPostFixture(
            GacSocialPlatform.Twitter,
            "tw-3",
            "تحقيقات جديدة في ثلاث منشآت في قطاع توزيع المواد الغذائية — حماية للمستهلك وللسوق. #منافسة_عادلة",
            "https://twitter.com/SaudiGAC/status/demo-3",
            3,
            892,
            76,
            234),
        new DemoSocialPostFixture(
            GacSocialPlatform.LinkedIn,
            "li-4",
            "يسرّنا تخرّج 45 محقق منافسة من الدورة التدريبية التخصصية في جدة. بناء القدرات الوطنية مستمر.",
            "https://linkedin.com/posts/gac-demo-4",
            4,
            267,
            12,
            28),
        new DemoSocialPostFixture(
            GacSocialPlatform.Twitter,
            "tw-5",
            "موافقة مشروطة على صفقة استحواذ في قطاع التأمين الصحي — لضمان حقوق المستفيدين.",
            "https://twitter.com/SaudiGAC/status/demo-5",
            5,
            445,
            41,
            78),
        new DemoSocialPostFixture(
            GacSocialPlatform.LinkedIn,
            "li-6",
            "المملكة تحقق تقدماً لافتاً في مؤشر المنافسة العالمي بـ 84% — حصيلة الجهود المشتركة مع رؤية 2030.",
            "https://linkedin.com/posts/gac-demo-6",
            6,
            723,
            52,
            156),
    };
}
