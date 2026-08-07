namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /shorfah/sections/{id}</c> (BUSINESS-RULES.md §1.4 field-level tiers).</summary>
/// <param name="ContentMd">The markdown content body, gated by contribute-or-higher.</param>
/// <param name="ContentHtml">The HTML content body, gated by contribute-or-higher.</param>
/// <param name="IncludeInPdf">Whether the section is included in the published PDF, gated by review-or-higher.</param>
/// <param name="TitleAr">The Arabic title, admin-only.</param>
/// <param name="DisplayOrder">The display sort order, admin-only.</param>
/// <param name="DescriptionAr">The Arabic description, admin-only.</param>
/// <param name="SlaDays">The SLA day count, admin-only.</param>
/// <param name="SlaStartsAt">The SLA clock start, admin-only.</param>
/// <param name="SlaDeadline">The SLA deadline, admin-only.</param>
public sealed record PatchShorfahSectionRequest(
    string? ContentMd,
    string? ContentHtml,
    bool? IncludeInPdf,
    string? TitleAr,
    int? DisplayOrder,
    string? DescriptionAr,
    int? SlaDays,
    DateTimeOffset? SlaStartsAt,
    DateTimeOffset? SlaDeadline);
