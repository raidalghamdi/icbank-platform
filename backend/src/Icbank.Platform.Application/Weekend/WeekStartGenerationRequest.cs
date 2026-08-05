namespace Icbank.Platform.Application.Weekend;

/// <summary>The generation request parameters (BUSINESS-RULES.md §2.5).</summary>
/// <param name="Topic">The message topic.</param>
/// <param name="Occasion">The occasion, if any.</param>
/// <param name="Audience">The target audience, if any.</param>
/// <param name="Tone">The desired tone (defaults to <c>ودية</c>).</param>
/// <param name="Length">The desired length option: <c>short</c>, <c>medium</c> (default), or <c>long</c>.</param>
/// <param name="StyleContext">A formatted style-profile digest used to steer generation.</param>
public sealed record WeekStartGenerationRequest(string Topic, string? Occasion, string? Audience, string? Tone, string? Length, string? StyleContext);
