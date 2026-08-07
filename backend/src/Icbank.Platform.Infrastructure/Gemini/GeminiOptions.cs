namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Strongly-typed Gemini configuration. The API key itself is intentionally not a property here
/// that gets bound/logged by name — <see cref="GeminiApiKeyResolver"/> reads it directly from
/// <c>IConfiguration</c> using the Node source's exact fallback chain (<c>GEMINI_API_KEY</c> then
/// <c>GOOGLE_AI_API_KEY</c> then <c>AI_INTEGRATIONS_GEMINI_API_KEY</c>) so that no options object,
/// log statement, or serialized DTO ever carries the secret in memory longer than one call.
/// </summary>
public sealed class GeminiOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Gemini";

    /// <summary>Default text model, matches the Node source's <c>GEMINI_TEXT_MODEL</c> default.</summary>
    public const string DefaultTextModel = "gemini-2.5-flash";

    /// <summary>Default "pro" model, matches the Node source's <c>GEMINI_PRO_MODEL</c> default.</summary>
    public const string DefaultProModel = "gemini-2.5-pro";

    /// <summary>Default image model, matches the Node source's <c>GEMINI_IMAGE_MODEL</c> default.</summary>
    public const string DefaultImageModel = "gemini-2.5-flash-image";

    /// <summary>Gets or sets the text generation model, env-overridable (<c>Gemini:TextModel</c>).</summary>
    public string TextModel { get; set; } = DefaultTextModel;

    /// <summary>Gets or sets the "pro" model, env-overridable (<c>Gemini:ProModel</c>).</summary>
    public string ProModel { get; set; } = DefaultProModel;

    /// <summary>Gets or sets the image-generation model, env-overridable (<c>Gemini:ImageModel</c>).</summary>
    public string ImageModel { get; set; } = DefaultImageModel;

    /// <summary>Gets or sets the base URL for the Gemini Generative Language REST API.</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
}
