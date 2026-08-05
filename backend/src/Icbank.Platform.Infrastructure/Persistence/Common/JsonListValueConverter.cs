using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Icbank.Platform.Infrastructure.Persistence.Common;

/// <summary>
/// Builds EF Core value converters and comparers for JSONB array columns (DATA-MODEL.md
/// section 6), e.g. <c>tags</c>/<c>suggestions</c> style <c>string[]</c>/<c>number[]</c> columns,
/// storing them as <c>nvarchar(max)</c> JSON text while keeping the CLR side a strongly-typed
/// <see cref="List{T}"/>.
/// </summary>
public static class JsonListValueConverter
{
    /// <summary>Creates a value converter that serializes <c>List&lt;T&gt;</c> to/from JSON text.</summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <returns>A configured <see cref="ValueConverter{TModel,TProvider}"/>.</returns>
    public static ValueConverter<List<T>, string> Create<T>() =>
        new(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => Deserialize<T>(json));

    /// <summary>Creates a value comparer that compares <c>List&lt;T&gt;</c> instances element-by-element.</summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <returns>A configured <see cref="ValueComparer{T}"/>.</returns>
    public static ValueComparer<List<T>> CreateComparer<T>() =>
        new(
            (left, right) => (left ?? new List<T>()).SequenceEqual(right ?? new List<T>()),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => value.ToList());

    private static List<T> Deserialize<T>(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? new List<T>()
            : JsonSerializer.Deserialize<List<T>>(json, (JsonSerializerOptions?)null) ?? new List<T>();
}
