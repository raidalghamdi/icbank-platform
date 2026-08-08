namespace Icbank.Platform.Domain.Designs;

/// <summary>Extra vocabulary used to match everyday administrative Arabic onto catalogue icons.</summary>
/// <remarks>
/// <see cref="IconLibrary"/>'s keywords describe each glyph — "ساعة", "تقويم", "مبنى" — because they
/// were written to drive a visual picker where the user already knows what they want. Staff copy
/// uses the vocabulary of policy instead ("استثناء", "لائحة", "مهلة"), which matched almost nothing,
/// so every list item fell through to the same generic glyph. These entries sit beside the
/// catalogue rather than inside it so the picker's own labels stay untouched.
/// </remarks>
public static class IconEventKeywordIndex
{
    /// <summary>Gets the supplementary keywords for each icon, keyed by catalogue name.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Supplementary { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["clock"] = new[] { "دوام", "انصراف", "ساعات", "تأخير", "مهلة", "استئذان", "شهري", "يومي", "فترة" },
            ["calendar"] = new[] { "شهر", "الشهر", "سنوي", "جدول", "مواعيد", "المهلة", "قبل", "خلال" },
            ["check-circle"] = new[] { "مراجعة", "تحقق", "اعتماد", "معتمدة", "استكمال", "التزام", "امتثال", "مطابقة", "منجز", "طلب", "الطلب", "رفع", "تقديم", "استثناء", "نموذج", "معاملة", "موافقة" },
            ["users"] = new[] { "الموظفين", "موظف", "الموظف", "فريق", "منسوبي", "كوادر", "زملاء" },
            ["user"] = new[] { "المدير", "مدير", "التنفيذي", "المسؤول", "الرئيس" },
            ["building"] = new[] { "إدارة", "الإدارة", "الهيئة", "قطاع", "جهة", "الموارد", "البشرية", "وحدة" },
            ["book-open"] = new[] { "لائحة", "نظام", "سياسة", "ضوابط", "الضوابط", "دليل", "إجراءات", "الإجراءات", "أحكام" },
            ["shield"] = new[] { "حفظ", "صون", "سجل", "سجلات", "خصوصية", "سلامة", "وقاية" },
            ["megaphone"] = new[] { "تذكير", "تعميم", "إشعار", "توعية", "بلاغ" },
            ["hand-heart"] = new[] { "رصيد", "ميزة", "مزايا", "دعم", "رعاية", "استحقاق" },
            ["trending-up"] = new[] { "كفاءة", "تحسين", "أداء", "إنتاجية", "ارتفاع", "زيادة" },
            ["target"] = new[] { "أولوية", "محور", "خطة", "مبادرة", "توجه" },
            ["chart-bar"] = new[] { "تقرير", "مؤشر", "نسبة", "معدل", "قياس", "تقارير" },
            ["mail"] = new[] { "مراسلة", "خطاب", "إرسال" },
            ["award"] = new[] { "تميز", "تقدير", "شكر", "مكافأة" },
            ["handshake"] = new[] { "شريك", "شراكة", "تكامل", "تنسيق" },
            ["map-pin"] = new[] { "مقر", "فرع", "قاعة" },
            ["graduation-cap"] = new[] { "برنامج", "دبلوم", "شهادات", "منح" },
            ["lightbulb"] = new[] { "مقترح", "تطوير", "حلول", "رأي" },
        };
}
