namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Contains parsed body blocks and records which contact values already appear inline.</summary>
internal sealed record IconEventParagraphFlow(IReadOnlyList<IconEventParagraphBlock> Blocks, bool EmailUsedInline, bool PhoneUsedInline);
