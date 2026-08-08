namespace Icbank.Platform.Domain.Designs;

/// <summary>A labelled block of body copy, such as <c>موعد رفع الطلب: يجب تقديم ...</c>.</summary>
/// <param name="Label">The heading text, without its trailing colon.</param>
/// <param name="Body">The copy that belongs under the heading.</param>
public sealed record IconEventTextSection(string Label, string Body);
