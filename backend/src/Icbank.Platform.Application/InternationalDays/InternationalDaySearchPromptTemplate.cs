using System.Globalization;
using System.Text;

namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// Carries the international-day AI-search prompt verbatim from the Node source
/// (BUSINESS-RULES.md §4.2, <c>international-days.ts:39-92</c>'s <c>buildPrompt()</c>). This
/// prompt text is product IP and must not be paraphrased -- every character, including the exact
/// Arabic wording and the JSON schema shown to the model, is preserved. Only <c>${dayName}</c>,
/// <c>${year}</c>, and the two derived <c>${year-1}</c>/<c>${year-2}</c> substitutions are
/// interpolated, exactly as the Node template literal did.
/// </summary>
public static class InternationalDaySearchPromptTemplate
{
    /// <summary>The system prompt Perplexity was called with (BUSINESS-RULES.md §4.3), verbatim.</summary>
    public const string PerplexitySystemPrompt = "أنت محلل بيانات متخصص. أرجع دائماً JSON منظم صالح فقط بدون أي نص إضافي أو markdown.";

    // Why: kept as a field (not inline in Build()) so the R-BE-091 40-line method-length gate
    // measures only the interpolation logic, not this verbatim, product-IP prompt text itself.
    private const string RawTemplate = """
ابحث عن "{0}" واستخرج بدقة المعلومات التالية، وأرجع النتيجة كـ JSON منظم فقط بدون أي نص إضافي خارج JSON:

{{
  "day_name_ar": "اسم اليوم بالعربية",
  "day_name_en": "Day name in English",
  "annual_date": "التاريخ السنوي مثل: 21 مارس",
  "official_organizer": "الجهة الراعية الرسمية دولياً مثل: منظمة العمل الدولية",
  "official_organizer_source": "رابط الصفحة الرسمية أو null",
  "history_summary": "ملخص تاريخي مختصر في 3 أسطر عن نشأة اليوم",
  "history_source": "رابط المصدر أو null",
  "current_theme_ar": "شعار/ثيم عام {1} بالعربية",
  "current_theme_en": "Theme of {1} in English",
  "theme_source_url": "رابط المصدر الرسمي للثيم أو null",
  "activations": [
    {{
      "entity_name": "اسم الجهة السعودية (وزارة أو هيئة أو شركة)",
      "entity_type": "حكومي أو خاص",
      "activation_type": "حملة أو فعالية أو منشور أو إنفوجرافيك",
      "platform": "اسم المنصة مثل: تويتر أو لينكدإن أو إنستغرام أو موقع رسمي أو يوتيوب",
      "description": "وصف موجز للتفعيل ومحتواه",
      "source_url": "رابط مباشر للمحتوى أو null",
      "year": {2}
    }}
  ],
  "design_samples": [
    {{
      "entity_name": "اسم الجهة التي نشرت التصميم",
      "entity_type": "حكومي أو خاص أو دولي",
      "platform": "اسم المنصة مثل: موقع رسمي أو تويتر أو إنستغرام أو لينكدإن أو فيسبوك",
      "description": "وصف التصميم أو الحملة البصرية ومضمونها",
      "page_url": "رابط المنشور أو الصفحة التي تحتوي التصميم أو null",
      "image_url": "رابط مباشر للصورة أو البوستر (ينتهي بـ .jpg أو .png أو .webp أو .gif) إن وجد أو null",
      "country": "البلد",
      "year": {2}
    }}
  ],
  "suggestions": [
    "فكرة تفعيل مقترحة قابلة للتطبيق في بيئة عمل حكومية سعودية"
  ],
  "sources": [
    {{"url": "رابط", "title": "عنوان المصدر", "publisher": "الناشر"}}
  ]
}}

تعليمات مهمة:
1. التفعيلات للجهات السعودية فقط (وزارات، هيئات، شركات كبرى) — لا تضمّن جهات من دول أخرى في حقل activations.
2. اجمع تفعيلات من الأعوام {3} و{2} و{1} فقط.
3. أنواع التفعيل المطلوبة حصراً: حملة (توعوية أو إعلانية)، فعالية (مؤتمر أو ورشة أو احتفالية)، منشور (محتوى سوشيال ميديا)، إنفوجرافيك (مادة بصرية توضيحية).
4. قدّم 8 إلى 15 تفعيلاً من جهات سعودية متنوعة موزعة على الأنواع الأربعة.
5. كل حقل source_url يجب أن يكون رابطاً حقيقياً أو null — لا تخترع روابط.
6. design_samples: 3-5 أمثلة بصرية من أي جهة موثقة بروابط.
7. اذكر 5 أفكار تفعيل مقترحة على الأقل.
8. أرجع JSON صالحاً فقط.
""";

    private static readonly CompositeFormat Template = CompositeFormat.Parse(RawTemplate);

    /// <summary>Builds the full user prompt for the given day name and current year, matching <c>buildPrompt()</c> verbatim.</summary>
    /// <param name="dayName">The day name to research.</param>
    /// <param name="year">The current year.</param>
    /// <returns>The fully interpolated prompt text.</returns>
    public static string Build(string dayName, int year)
    {
        var previousYear = (year - 1).ToString(CultureInfo.InvariantCulture);
        var twoYearsAgo = (year - 2).ToString(CultureInfo.InvariantCulture);
        var yearText = year.ToString(CultureInfo.InvariantCulture);
        return string.Format(CultureInfo.InvariantCulture, Template, dayName, yearText, previousYear, twoYearsAgo);
    }
}
