using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Icbank.Platform.Infrastructure.Persistence.Common;

/// <summary>
/// Builds EF Core value converters and comparers for nullable JSONB array columns
/// (DATA-MODEL.md section 6), e.g. <c>archive_entries.embedding</c>, storing them as
/// <c>nvarchar(max)</c> JSON text while keeping the CLR side a strongly-typed, nullable
/// <see cref="List{T}"/>.
/// </summary>
public static class NullableJsonListValueConverter
{
    /// <summary>Creates a value converter that serializes a nullable <c>List&lt;T&gt;</c> to/from JSON text.</summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <returns>A configured <see cref="ValueConverter{TModel,TProvider}"/>.</returns>
    public static ValueConverter<List<T>?, string?> Create<T>() =>
        new(
            list => list == null ? null : JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<T>>(json, (JsonSerializerOptions?)null));

    /// <summary>Creates a value comparer that compares nullable <c>List&lt;T&gt;</c> instances element-by-element.</summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <returns>A configured <see cref="ValueComparer{T}"/>.</returns>
    public static ValueComparer<List<T>?> CreateComparer<T>() =>
        new(
            (left, right) => Equals(left, right) || (left != null && right != null && left.SequenceEqual(right)),
            value => value == null ? 0 : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => value == null ? null : value.ToList());
}
