namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of a <see cref="SeedDemoNewsCommand"/>.</summary>
/// <param name="Message">A human-readable Arabic confirmation message.</param>
/// <param name="SeededNews">The number of demo news items inserted.</param>
/// <param name="SeededPosts">The number of demo social posts inserted.</param>
public sealed record SeedDemoNewsResultDto(string Message, int SeededNews, int SeededPosts);
