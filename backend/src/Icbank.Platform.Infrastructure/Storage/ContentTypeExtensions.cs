namespace Icbank.Platform.Infrastructure.Storage;

/// <summary>Resolves a safe file extension from a MIME content type, never trusting a client-supplied file name (SEC-17).</summary>
public static class ContentTypeExtensions
{
    private static readonly Dictionary<string, string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["text/html"] = ".html",
        ["text/html; charset=utf-8"] = ".html",
    };

    /// <summary>Resolves the safe extension for the given content type, defaulting to <c>.bin</c> when unknown.</summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>A leading-dot file extension.</returns>
    public static string Resolve(string contentType) =>
        KnownExtensions.TryGetValue(contentType, out var extension) ? extension : ".bin";
}
