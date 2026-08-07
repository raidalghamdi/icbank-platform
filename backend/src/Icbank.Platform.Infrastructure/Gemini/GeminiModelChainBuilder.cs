namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Ports <c>buildModelChain(primary)</c> from <c>aiProviders.ts</c> verbatim: start with the
/// caller's primary model, then append <c>gemini-2.5-flash</c>, <c>gemini-2.5-flash-lite</c>, and
/// <c>gemini-flash-latest</c> in that fixed order, skipping any that duplicate a model already in
/// the chain (case-sensitive, exact string match, exactly like the Node source's
/// <c>if (!chain.includes(m))</c>).
/// </summary>
public static class GeminiModelChainBuilder
{
    private static readonly string[] FallbackModelsInOrder = { "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-flash-latest" };

    /// <summary>Builds the ordered, duplicate-free model fallback chain for a given primary model.</summary>
    /// <param name="primaryModel">The caller's requested/default model, always first in the chain.</param>
    /// <returns>The ordered list of models to try, primary first.</returns>
    public static IReadOnlyList<string> Build(string primaryModel)
    {
        var chain = new List<string> { primaryModel };
        foreach (var fallback in FallbackModelsInOrder)
        {
            if (!chain.Contains(fallback, StringComparer.Ordinal))
            {
                chain.Add(fallback);
            }
        }

        return chain;
    }
}
