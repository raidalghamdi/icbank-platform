namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/issues/{issueId}/sections</c>.</summary>
/// <param name="SectionType">The section type.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DisplayOrder">The optional display order.</param>
/// <param name="OwnerUserId">The optional owning user's id.</param>
/// <param name="OwnerRole">The optional owning role name.</param>
/// <param name="AutoGenerate">Whether the section content is AI-auto-generated.</param>
/// <param name="GenerationPrompt">An optional custom AI prompt override.</param>
/// <param name="ParentSectionId">The optional parent section's id.</param>
/// <param name="SlaDays">An explicit SLA-day override.</param>
public sealed record AddShorfahSectionRequest(
    string SectionType,
    string TitleAr,
    string? DescriptionAr,
    int? DisplayOrder,
    int? OwnerUserId,
    string? OwnerRole,
    bool AutoGenerate,
    string? GenerationPrompt,
    int? ParentSectionId,
    int? SlaDays);
