using System.Globalization;
using System.Text;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Carries the canonical 8-section GAC final-report prompt verbatim from BUSINESS-RULES.md §5.3
/// (<c>final-media-reports.ts:465-612</c>). The strict output rules footer (JSON-only, no
/// invented numbers, empty arrays/null when no data, formal Arabic) is preserved exactly.
/// </summary>
public static class FinalReportPromptTemplate
{
    private const string RawTemplate = """
أنت محلل إعلامي خبير يعمل لدى الهيئة العامة للمنافسة في المملكة العربية السعودية. أعدّ تقريراً إعلامياً رسمياً شاملاً من 8 أقسام بصيغة JSON بناءً على البيانات التالية.

الفترة: {0}
الجمهور المستهدف: {1}
{2}
البيانات المرصودة:
{3}

أخرج JSON واحد فقط بهذا الشكل بالضبط (بدون أي نص أو markdown قبله أو بعده):

{{
  "executiveSummary": "ملخص تنفيذي شامل من 4-6 جمل",
  "kpis": {{"totalNews":0,"positivePercent":0,"mediaOutlets":0,"keyTopics":0,"reach":"رقم تقريبي مع وحدة","alertsCount":0}},
  "topNews": [
    {{"date":"","tone":"","headline":"","details":[""],"source":""}}
  ],
  "timeline": [
    {{"date":"","event":"","outlet":"","tone":"","count":0}}
  ],
  "digitalPresence": {{
    "platforms": [
      {{"name":"إكس","mentions":0,"reposts":0,"engagement":0,"reach":""}},
      {{"name":"لينكدإن","mentions":0,"reposts":0,"engagement":0,"reach":""}},
      {{"name":"تليجرام","mentions":0,"reposts":0,"engagement":0,"reach":""}},
      {{"name":"يوتيوب","mentions":0,"reposts":0,"engagement":0,"reach":""}}
    ],
    "hashtags": [{{"tag":"","uses":0,"trend":""}}]
  }},
  "editorialTone": {{
    "distribution": [{{"label":"إيجابي","percent":0,"count":0}},{{"label":"محايد","percent":0,"count":0}},{{"label":"سلبي","percent":0,"count":0}}],
    "classification": [{{"label":"","percent":0,"count":0}}],
    "sources": [{{"label":"","percent":0,"count":0}}]
  }},
  "deepAnalysis": {{
    "keywords": [{{"keyword":"","frequency":0,"context":""}}],
    "quote": {{"text":"","source":"","date":""}},
    "strengths": [""],
    "weaknesses": [""]
  }},
  "regionalComparison": [
    {{"authority":"","country":"","mentions":0,"tone":"","highlights":""}}
  ],
  "recommendations": [
    {{"title":"","description":"","priority":"","responsible":"","kpi":"","deadline":"","dependencies":""}}
  ],
  "alerts": [
    {{"alert":"","suggestedPosition":""}}
  ],
  "quotesAppendix": [
    {{"quote":"","source":"","date":"","topic":""}}
  ],
  "methodology": "شرح منهجية الرصد والتحليل",
  "sources": [
    {{"name":"","url":"","description":""}}
  ]
}}

الأعداد المطلوبة:
- topNews: 5-8 عناصر
- timeline: كل التواريخ الرئيسية
- hashtags: 4-7 عناصر
- editorialTone.classification: 4-6 مواضيع
- editorialTone.sources: 4 فئات مصادر ثابتة
- deepAnalysis.keywords: 6-10 عناصر
- deepAnalysis.strengths: 3 عناصر
- deepAnalysis.weaknesses: 2 عناصر
- regionalComparison: 3-5 عناصر
- recommendations: 4-6 عناصر
- alerts: 2-4 عناصر
- quotesAppendix: 4-8 عناصر

⚠️ قواعد صارمة:
- JSON صالح 100% فقط، بدون markdown أو أي نص إضافي
- لا تخترع أرقاماً أو إحصاءات غير مستندة إلى البيانات المرصودة
- إذا لم تتوفر بيانات لعنصر ما، استخدم مصفوفة فارغة [] أو null، ولا تخترع محتوى
- استخدم العربية الفصحى الرسمية في جميع الحقول النصية
""";

    private static readonly CompositeFormat Template = CompositeFormat.Parse(RawTemplate);

    /// <summary>Builds the full prompt text.</summary>
    /// <param name="periodLabel">The human-readable period label.</param>
    /// <param name="audience">The target audience description.</param>
    /// <param name="focusTopics">The optional focus-topics free text; produces an extra prompt line only when non-empty.</param>
    /// <param name="formattedFeed">The flat numbered text block of source posts/news items.</param>
    /// <returns>The fully interpolated prompt text.</returns>
    public static string Build(string periodLabel, string audience, string? focusTopics, string formattedFeed)
    {
        var focusLine = string.IsNullOrWhiteSpace(focusTopics) ? string.Empty : $"المواضيع المحورية: {focusTopics}\n";
        return string.Format(CultureInfo.InvariantCulture, Template, periodLabel, audience, focusLine, formattedFeed);
    }
}
