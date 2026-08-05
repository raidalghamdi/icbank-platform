using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>One canonical section template row (BUSINESS-RULES.md §1.2).</summary>
/// <param name="SectionType">The section type.</param>
/// <param name="TitleAr">The fixed Arabic title.</param>
/// <param name="DescriptionAr">The fixed Arabic description.</param>
/// <param name="DisplayOrder">The fixed display order.</param>
public sealed record ShorfahCanonicalSectionTemplate(ShorfahSectionType SectionType, string TitleAr, string DescriptionAr, int DisplayOrder);
