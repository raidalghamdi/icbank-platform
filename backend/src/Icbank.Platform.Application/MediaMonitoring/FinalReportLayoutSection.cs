namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>One section of a report layout.</summary>
/// <param name="Key">The section key: a built-in key or <c>custom-…</c>.</param>
/// <param name="Title">The heading shown in the report; null or blank uses the built-in title.</param>
/// <param name="Visible">Whether the section appears in the preview and the PDF.</param>
/// <param name="Body">The text of a reviewer-added section; ignored for built-in sections.</param>
public sealed record FinalReportLayoutSection(string Key, string? Title, bool Visible, string? Body);
