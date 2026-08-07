using System.Globalization;
using System.Text;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Carries the weekend-draft AI generation prompt verbatim from BUSINESS-RULES.md §2.3
/// (<c>weekend-drafts.ts:100-144</c>). Riyadh is hardcoded into the prompt text itself, exactly as
/// the Node source did — a deliberate "Riyadh only" product scope limit, not parameterized.
/// </summary>
public static class WeekendGenerationPromptTemplate
{
    // Why: kept as a field (not inline in Build()) purely so the R-BE-091 40-line method-length
    // gate measures only the interpolation call, not this verbatim, product-IP prompt text.
    private const string RawTemplate = """
أنت محرر محتوى ترفيهي خبير للموظفين الحكوميين في المملكة العربية السعودية. مهمتك إنتاج محتوى نهاية أسبوع جاهز للنشر يخص مدينة الرياض ليوم الخميس {0} ولكامل الوينكد.

الأسلوب المطلوب:
- عربية فصحى راقية بنبرة ودودة احترافية مناسبة لجمهور حكومي
- عناوين جذابة وأوصاف ملموسة (لا عبارات عامة)
- كل وصف 2-3 أسطر يبرز ما يميز الخيار فعلياً
- استخدام أرقام وتفاصيل دقيقة (مواعيد، أسعار تقريبية، مواقع)

أخرج JSON واحد فقط بهذا الشكل بالضبط:

{{
  "summary": "فقرة 3 أسطر ترحيبية تلخص أبرز خيارات الوينكد بأسلوب جذاب",
  "places": [
    {{"title":"اسم المكان","body":"وصف 2-3 أسطر يبرز ما يميزه للعائلة مع تفاصيل ملموسة","maps_query":"Google Maps search query in English"}}
  ],
  "deals": [
    {{"title":"فئة العروض","items":[
      {{"place":"اسم العلامة التجارية","discount":"النسبة أو الميزة","detail":"شرح مفيد للعميل","emoji":"🍔"}}
    ]}}
  ],
  "podcasts": [
    {{"title":"اسم البودكاست","field":"المجال","episode":"اسم الحلقة","body":"وصف الحلقة وقيمتها","channel":"المنصة (Spotify/YouTube/Anghami)","tagline":"شعار قصير جذاب"}}
  ],
  "matches": [
    {{"title":"اسم البطولة","teams":"الفريق الأول × الفريق الثاني","time":"وقت المباراة بتوقيت الرياض","channel":"القناة الناقلة"}}
  ],
  "movies": [
    {{"title":"اسم الفيلم","genre":"النوع","cinema":"اسم السينما (Muvi أو VOX)","rating":"التصنيف العمري","body":"وصف سطر للعائلة"}}
  ]
}}

الأعداد المطلوبة بالضبط:
- places: 4 أماكن متنوعة (متحف/حديقة/سوق/فعالية موسمية)
- deals: 3 فئات، كل فئة 3 عروض من علامات معروفة في السعودية
- podcasts: 3 بودكاستات عربية معروفة فعلياً
- matches: 3 مباريات بارزة (دوري روشن/بطولات إقليمية)
- movies: 3 أفلام عائلية معاصرة

⚠️ تنبيهات حرجة:
- المدينة: الرياض حصراً
- جميع الخيارات حقيقية ومتاحة فعلياً
- علامات معروفة بالسعودية (Shake Shack, Starbucks, H&M, VOX, Muvi, Fitness Time, Almosafer...)
- لا تذكر أدوات ذكاء اصطناعي مطلقاً
- JSON صالح 100% بدون أي نص أو markdown قبله أو بعده
""";

    private static readonly CompositeFormat Template = CompositeFormat.Parse(RawTemplate);

    /// <summary>Builds the full prompt for the given weekend date.</summary>
    /// <param name="weekendDate">The ISO date string for the target Thursday.</param>
    /// <returns>The fully interpolated prompt text.</returns>
    public static string Build(string weekendDate) => string.Format(CultureInfo.InvariantCulture, Template, weekendDate);
}
