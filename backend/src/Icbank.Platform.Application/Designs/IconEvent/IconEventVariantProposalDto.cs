namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>The typed shape of one AI-proposed design variant, before code-enforced post-processing (H-2).</summary>
/// <param name="Layout">The AI-selected layout key, e.g. <c>stats-hero</c>.</param>
/// <param name="MainIcon">The AI-selected main icon name.</param>
/// <param name="SupportingIcons">The AI-selected supporting icon names.</param>
/// <param name="Rationale">The AI's explanation for this layout/icon choice.</param>
public sealed record IconEventVariantProposalDto(string Layout, string MainIcon, IReadOnlyList<string> SupportingIcons, string Rationale);
