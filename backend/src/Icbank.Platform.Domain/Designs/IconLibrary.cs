namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The static icon-event icon catalogue, ported verbatim (name/label/category/keywords) from
/// <c>composer/icon-library.ts</c>'s <c>ICON_LIBRARY</c> constant (BUSINESS-RULES.md §7.4). Backs
/// <c>GET /designs/icon-event/icons</c> and the AI-selection validation in
/// <c>GenerateIconEventDesignCommandHandler</c>.
/// </summary>
public static class IconLibrary
{
    /// <summary>Gets every catalogued icon, in source order.</summary>
    public static IReadOnlyList<IconDefinition> All { get; } = new List<IconDefinition>
    {
        new IconDefinition("graduation-cap", "تخرج / تعليم", IconCategory.Workshop, new[] { "ورشة", "تعليم", "تدريب", "تعلم", "أكاديمي" }),
        new IconDefinition("book-open", "كتاب", IconCategory.Workshop, new[] { "كتاب", "قراءة", "محتوى", "دورة" }),
        new IconDefinition("lightbulb", "فكرة", IconCategory.Workshop, new[] { "فكرة", "ابتكار", "إلهام", "ابداع" }),
        new IconDefinition("presentation", "عرض تقديمي", IconCategory.Workshop, new[] { "عرض", "بريزنتيشن", "محاضرة", "شرح" }),
        new IconDefinition("code", "كود برمجي", IconCategory.Workshop, new[] { "برمجة", "كود", "تطوير", "تقنية" }),
        new IconDefinition("brain", "ذكاء اصطناعي", IconCategory.Workshop, new[] { "ذكاء", "AI", "تفكير", "عقل", "اصطناعي" }),
        new IconDefinition("target", "هدف", IconCategory.Workshop, new[] { "هدف", "تركيز", "أهداف", "غاية" }),
        new IconDefinition("trending-up", "تطور / نمو", IconCategory.Workshop, new[] { "نمو", "تطور", "تقدم", "صعود" }),
        new IconDefinition("award", "شهادة / إنجاز", IconCategory.Workshop, new[] { "شهادة", "جائزة", "إنجاز", "تكريم" }),
        new IconDefinition("users", "مجموعة", IconCategory.Meeting, new[] { "مجموعة", "فريق", "حضور", "أشخاص" }),
        new IconDefinition("mic", "ميكروفون", IconCategory.Meeting, new[] { "ميكروفون", "خطاب", "صوت", "متحدث" }),
        new IconDefinition("podium", "منصة", IconCategory.Meeting, new[] { "منصة", "محاضرة", "خطابة" }),
        new IconDefinition("building", "مبنى / قاعة", IconCategory.Meeting, new[] { "مبنى", "قاعة", "مكان", "موقع" }),
        new IconDefinition("handshake", "مصافحة / اتفاق", IconCategory.Meeting, new[] { "مصافحة", "شراكة", "اتفاق", "تعاون" }),
        new IconDefinition("video", "اجتماع مرئي", IconCategory.Meeting, new[] { "فيديو", "زوم", "اجتماع", "افتراضي" }),
        new IconDefinition("message-circle", "محادثة", IconCategory.Meeting, new[] { "محادثة", "نقاش", "حوار", "كلام" }),
        new IconDefinition("globe", "عالمي", IconCategory.Meeting, new[] { "عالمي", "دولي", "عالم", "مؤتمر" }),
        new IconDefinition("rocket", "صاروخ / إطلاق", IconCategory.Launch, new[] { "إطلاق", "بداية", "انطلاقة", "صاروخ" }),
        new IconDefinition("megaphone", "إعلان", IconCategory.Launch, new[] { "إعلان", "نشر", "تنبيه", "بث" }),
        new IconDefinition("sparkles", "تميز / جديد", IconCategory.Launch, new[] { "جديد", "مميز", "نجوم", "إطلاق" }),
        new IconDefinition("star", "نجمة", IconCategory.Launch, new[] { "نجمة", "تميز", "أفضل", "مفضل" }),
        new IconDefinition("zap", "طاقة / سرعة", IconCategory.Launch, new[] { "طاقة", "سرعة", "برق", "قوة" }),
        new IconDefinition("trophy", "كأس / نجاح", IconCategory.Launch, new[] { "كأس", "نجاح", "فوز", "تتويج" }),
        new IconDefinition("flag", "علم / مرحلة", IconCategory.Launch, new[] { "علم", "مرحلة", "إنجاز", "هدف" }),
        new IconDefinition("gift", "هدية", IconCategory.Launch, new[] { "هدية", "مفاجأة", "عرض", "جائزة" }),
        new IconDefinition("party-popper", "احتفال", IconCategory.Social, new[] { "احتفال", "حفلة", "اجتماعي", "فرح" }),
        new IconDefinition("heart", "حب / اهتمام", IconCategory.Social, new[] { "حب", "قلب", "اهتمام", "عاطفة" }),
        new IconDefinition("coffee", "قهوة / استراحة", IconCategory.Social, new[] { "قهوة", "استراحة", "لقاء", "كوفي" }),
        new IconDefinition("music", "موسيقى", IconCategory.Social, new[] { "موسيقى", "غناء", "ترفيه", "نغمة" }),
        new IconDefinition("cake", "كيك / مناسبة", IconCategory.Social, new[] { "كيك", "حفلة", "مناسبة", "ميلاد" }),
        new IconDefinition("smile", "ابتسامة", IconCategory.Social, new[] { "ابتسامة", "سعادة", "فرح", "إيجابي" }),
        new IconDefinition("users-round", "تجمع", IconCategory.Social, new[] { "تجمع", "أصدقاء", "اجتماع", "مجتمع" }),
        new IconDefinition("hand-heart", "تطوع / عطاء", IconCategory.Social, new[] { "تطوع", "عطاء", "خير", "مساعدة" }),
        new IconDefinition("calendar", "تقويم / تاريخ", IconCategory.Common, new[] { "تاريخ", "تقويم", "موعد", "يوم" }),
        new IconDefinition("clock", "ساعة / وقت", IconCategory.Common, new[] { "وقت", "ساعة", "زمن", "موعد" }),
        new IconDefinition("map-pin", "موقع / مكان", IconCategory.Common, new[] { "موقع", "مكان", "خريطة", "عنوان" }),
        new IconDefinition("user", "شخص", IconCategory.Common, new[] { "شخص", "متحدث", "مستخدم", "فرد" }),
        new IconDefinition("ticket", "تذكرة", IconCategory.Common, new[] { "تذكرة", "حجز", "دخول", "تسجيل" }),
        new IconDefinition("qr-code", "QR / مسح", IconCategory.Common, new[] { "QR", "مسح", "كود", "تسجيل" }),
        new IconDefinition("check-circle", "تأكيد / صح", IconCategory.Common, new[] { "تأكيد", "صح", "إنجاز", "نجاح" }),
        new IconDefinition("phone", "اتصال", IconCategory.Common, new[] { "اتصال", "هاتف", "تواصل", "رقم" }),
        new IconDefinition("mail", "بريد", IconCategory.Common, new[] { "بريد", "إيميل", "رسالة", "تواصل" }),
        new IconDefinition("link", "رابط", IconCategory.Common, new[] { "رابط", "اتصال", "وصلة" }),
        new IconDefinition("shield", "حماية", IconCategory.Common, new[] { "حماية", "أمان", "سرية", "ضمان" }),
        new IconDefinition("chart-bar", "إحصائيات", IconCategory.Common, new[] { "إحصائيات", "بيانات", "رسم", "نتائج" }),
        new IconDefinition("globe-arabic", "السعودية", IconCategory.Common, new[] { "سعودية", "وطن", "بلد", "محلي" }),
    };

    /// <summary>Gets the set of every valid icon name, for O(1) validation lookups.</summary>
    public static IReadOnlySet<string> ValidNames { get; } =
        new HashSet<string>(All.Select(icon => icon.Name), StringComparer.Ordinal);
}
