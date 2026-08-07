using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Verbatim per-section-type AI generation guidance fragments and envelope template
/// (BUSINESS-RULES.md §1.8, <c>shorfah.ts:471-513</c>). Product IP: carried over
/// character-for-character, never re-derived or paraphrased.
/// </summary>
public static class ShorfahGenerationPrompts
{
    /// <summary>Fallback guidance used for any section type with no dedicated fragment below.</summary>
    public const string FallbackGuidance = "اكتب محتوى مناسباً للقسم.";

    private static readonly Dictionary<ShorfahSectionType, string> GuidanceByType = new()
    {
        [ShorfahSectionType.News] = "اكتب 3-4 فقرات أخبارية عن أبرز فعاليات وأخبار الهيئة هذا الشهر. كل فقرة بعنوان H3 وفقرة وصفية قصيرة (60-100 كلمة). النبرة رسمية وموجزة.",
        [ShorfahSectionType.OfficeInterview] = "اكتب مسودة حوار مع أحد القياديين التنفيذيين في الهيئة، تظهر رؤيته لإدارته وأهدافها. تتألف من: اسم الضيف، عنوان رئيسي، ثم 4-6 أسئلة بعناوين H3 مع إجابات تفصيلية (80-150 كلمة للإجابة).",
        [ShorfahSectionType.CompetitionCulture] = "أعد لائحة مؤشرات وأرقام عن جهود نشر ثقافة المنافسة خلال الشهر: عدد اللقاءات، عدد المستفيدين، عدد منشورات التواصل، طلبات الخدمات الإلكترونية، نسبة الرضا، مستفيدين من مركز الاتصال. اجعلها في فقرة افتتاحية قصيرة + قائمة بـ 8-10 مؤشرات.",
        [ShorfahSectionType.OutsideBox] = "اكتب مقالاً إبداعياً بقلم موظف خبير في أحد المجالات (على سبيل المثال: الموارد البشرية، المالية، أو التحول الرقمي). البداية باسم الضيف ومنصبه وعنوان جذاب، ثم مقال من 4-5 فقرات (300-450 كلمة).",
        [ShorfahSectionType.Events] = "أعد قائمة فعاليات الهيئة لهذا الشهر بعنوان H3 لكل فعالية + فقرة وصفية قصيرة (40-80 كلمة). العدد من 3 إلى 5 فعاليات.",
        [ShorfahSectionType.EmployeeQa] = "اختر أحد الموظفين وأجر معه حواراً سريعاً: اسمه ومنصبه، ثم 6 أسئلة سريعة وإجابات قصيرة (جملة أو جملتين). استخدم صيغة: **س: السؤال** ... **ج: الإجابة**.",
    };

    /// <summary>Resolves the guidance fragment for a section type, falling back to <see cref="FallbackGuidance"/>.</summary>
    /// <param name="sectionType">The section type.</param>
    /// <returns>The guidance fragment.</returns>
    public static string GuidanceFor(ShorfahSectionType sectionType) =>
        GuidanceByType.TryGetValue(sectionType, out var guidance) ? guidance : FallbackGuidance;

    /// <summary>Builds the full envelope prompt for a section, verbatim (<c>shorfah.ts:512-519</c>). If <paramref name="customPrompt"/> is set, it is used as-is instead.</summary>
    /// <param name="titleAr">The section's Arabic title.</param>
    /// <param name="descriptionAr">The section's optional Arabic description.</param>
    /// <param name="sectionType">The section type, used to select the guidance fragment.</param>
    /// <param name="customPrompt">A per-section custom prompt override, used verbatim instead of the guidance envelope when set.</param>
    /// <returns>The full prompt to send to the AI provider.</returns>
    public static string BuildPrompt(string titleAr, string? descriptionAr, ShorfahSectionType sectionType, string? customPrompt)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        var guidance = GuidanceFor(sectionType);
        return $"أنت محرر مجلة \"شُرفة\" الشهرية الداخلية للهيئة العامة للمنافسة السعودية.\n" +
               $"القسم: \"{titleAr}\"\n" +
               $"الوصف: {descriptionAr ?? string.Empty}\n" +
               $"المطلوب: {guidance}\n" +
               "النبرة: رسمية، احترافية، عربية فصحى واضحة.\n" +
               "أرجع JSON بهذا الشكل فقط: { \"content_md\": \"محتوى ماركداون بالعربية\" }";
    }
}
