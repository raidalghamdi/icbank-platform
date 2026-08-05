using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Icbank.Platform.Infrastructure.Persistence.Common;

/// <summary>
/// Builds EF Core value converters and comparers for JSONB-sourced columns (DATA-MODEL.md
/// section 6), storing them as <c>nvarchar(max)</c> JSON text while keeping the CLR side
/// strongly typed. Used from every <c>IEntityTypeConfiguration&lt;T&gt;</c> that maps a JSON
/// column instead of raw strings, per the task's typed-JSON requirement. The converter accepts
/// nullable CLR values so it can back both required and optional JSON columns.
/// </summary>
public static class JsonValueConverter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Creates a value converter that serializes <typeparamref name="T"/> to/from JSON text.</summary>
    /// <typeparam name="T">The strongly-typed CLR shape stored in the column.</typeparam>
    /// <returns>A configured <see cref="ValueConverter{TModel,TProvider}"/>.</returns>
    public static ValueConverter<T?, string> Create<T>()
        where T : class, new() =>
        new(
            model => Serialize(model),
            json => Deserialize<T>(json));

    /// <summary>Creates a value converter for a column whose CLR property is non-nullable (has a default instance).</summary>
    /// <typeparam name="T">The strongly-typed CLR shape stored in the column.</typeparam>
    /// <returns>A configured <see cref="ValueConverter{TModel,TProvider}"/>.</returns>
    public static ValueConverter<T, string> CreateRequired<T>()
        where T : class, new() =>
        new(
            model => Serialize(model),
            json => Deserialize<T>(json));

    /// <summary>Creates a value comparer that compares <typeparamref name="T"/> instances by their serialized JSON.</summary>
    /// <typeparam name="T">The strongly-typed CLR shape stored in the column.</typeparam>
    /// <returns>A configured <see cref="ValueComparer{T}"/>.</returns>
    public static ValueComparer<T?> CreateComparer<T>()
        where T : class, new() =>
        new(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => value == null ? null : Deserialize<T>(Serialize(value)));

    /// <summary>Creates a value comparer for a column whose CLR property is non-nullable.</summary>
    /// <typeparam name="T">The strongly-typed CLR shape stored in the column.</typeparam>
    /// <returns>A configured <see cref="ValueComparer{T}"/>.</returns>
    public static ValueComparer<T> CreateRequiredComparer<T>()
        where T : class, new() =>
        new(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => Deserialize<T>(Serialize(value)));

    private static string Serialize<T>(T? value)
        where T : class =>
        value is null ? string.Empty : JsonSerializer.Serialize(value, SerializerOptions);

    private static T Deserialize<T>(string json)
        where T : class, new() =>
        string.IsNullOrWhiteSpace(json)
            ? new T()
            : JsonSerializer.Deserialize<T>(json, SerializerOptions) ?? new T();
}
