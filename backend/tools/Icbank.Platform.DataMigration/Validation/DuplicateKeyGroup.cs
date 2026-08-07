namespace Icbank.Platform.DataMigration.Validation;

/// <summary>One key value that more than one source row maps to under a new destination unique index.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <param name="Key">The duplicated key value.</param>
/// <param name="SourceIds">Every source row id sharing this key, in source order.</param>
public sealed record DuplicateKeyGroup<TKey>(TKey Key, IReadOnlyList<int> SourceIds);
