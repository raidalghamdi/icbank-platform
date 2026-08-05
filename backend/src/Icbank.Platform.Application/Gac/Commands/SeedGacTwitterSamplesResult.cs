namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The seed outcome summary.</summary>
/// <param name="Inserted">The number of newly inserted sample posts.</param>
/// <param name="Skipped">The number of sample posts skipped because they already existed.</param>
/// <param name="Total">The total number of sample posts in the fixture set.</param>
public sealed record SeedGacTwitterSamplesResult(int Inserted, int Skipped, int Total);
