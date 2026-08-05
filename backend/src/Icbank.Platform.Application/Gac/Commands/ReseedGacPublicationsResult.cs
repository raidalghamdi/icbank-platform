namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The reseed outcome summary.</summary>
/// <param name="Inserted">The number of newly inserted publications.</param>
/// <param name="Skipped">The titleAr values skipped because a row already existed.</param>
public sealed record ReseedGacPublicationsResult(int Inserted, IReadOnlyList<string> Skipped);
