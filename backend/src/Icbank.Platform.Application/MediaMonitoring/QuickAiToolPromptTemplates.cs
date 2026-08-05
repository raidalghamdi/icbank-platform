using System.Globalization;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// The 7 fixed "AI Quick" tool prompt templates (BUSINESS-RULES.md §5.6, verbatim from
/// <c>media-monitoring.ts:489-514</c>). Carried over exactly as product IP -- these are the
/// literal Arabic prompt strings the Node source sent to Gemini, not paraphrased.
/// </summary>
public static class QuickAiToolPromptTemplates
{
    /// <summary>The default number of headlines to suggest when the caller does not specify a count (BUSINESS-RULES.md §5.6).</summary>
    public const int DefaultHeadlineCount = 8;

    /// <summary>Builds the prompt for the given tool key. Returns <c>null</c> for an unrecognized tool key.</summary>
    /// <param name="tool">The tool key: <c>generate</c>, <c>tone</c>, <c>rephrase</c>, <c>rewrite</c>, <c>headlines</c>, <c>summary</c>, or <c>messages</c>.</param>
    /// <param name="input">The caller-supplied input text.</param>
    /// <param name="tone">The optional requested tone, used by <c>generate</c>, <c>tone</c>, and <c>messages</c>.</param>
    /// <param name="count">The optional requested count, used by <c>headlines</c>.</param>
    /// <returns>The fully-built prompt text, or <c>null</c> if <paramref name="tool"/> is not one of the 7 fixed tools.</returns>
    public static string? Build(string tool, string input, string? tone, int? count) => tool switch
    {
        "generate" => BuildGenerate(input, tone),
        "tone" => BuildTone(input, tone),
        "rephrase" => $"حسّن صياغة هذه الفقرة لتكون أكثر وضوحاً واحترافية:\n\n{input}",
        "rewrite" => $"أعد كتابة النص التالي بأسلوب مختلف مع الحفاظ على الرسالة الأساسية:\n\n{input}",
        "headlines" => BuildHeadlines(input, count),
        "summary" => $"لخّص النص التالي في 3–5 نقاط رئيسية:\n\n{input}",
        "messages" => BuildMessages(input, tone),
        _ => null,
    };

    private static string BuildGenerate(string input, string? tone)
    {
        var toneSuffix = string.IsNullOrWhiteSpace(tone) ? string.Empty : $" بنبرة {tone}";
        return $"أنت محرر محتوى محترف في هيئة حكومية. اكتب محتوى عربي واضح ومتماسك عن الموضوع التالي{toneSuffix}:\n\n{input}";
    }

    private static string BuildTone(string input, string? tone)
    {
        var resolvedTone = string.IsNullOrWhiteSpace(tone) ? "رسمية" : tone;
        return $"أعد صياغة النص التالي بنبرة {resolvedTone}، مع الحفاظ على المعنى:\n\n{input}";
    }

    private static string BuildHeadlines(string input, int? count)
    {
        var resolvedCount = count ?? DefaultHeadlineCount;
        return $"اقترح {resolvedCount.ToString(CultureInfo.InvariantCulture)} عناوين إعلامية جذّابة ومولحة لمحتوى عن:\n\n{input}\n\nرتّبها في قائمة مرقّمة.";
    }

    private static string BuildMessages(string input, string? tone)
    {
        var toneSuffix = string.IsNullOrWhiteSpace(tone) ? string.Empty : $" وبنبرة {tone}";
        return $"حسّن رسالة التواصل التالية لتكون أكثر احترافية ووضوحاً{toneSuffix}:\n\n{input}";
    }
}
