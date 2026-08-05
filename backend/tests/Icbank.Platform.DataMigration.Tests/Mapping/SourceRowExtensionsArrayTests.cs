using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// <see cref="SourceRowExtensions.GetStringArray"/>, <see cref="SourceRowExtensions.GetInt32Array"/>
/// and <see cref="SourceRowExtensions.GetFloatArray"/> back every array-fan-out migrator (e.g.
/// <see cref="Icbank.Platform.DataMigration.Migration.Migrators.AiYearActivationTableMigrator"/>'s
/// <c>channels</c> child-table normalization). Before this test class none of the three had any
/// coverage at all -- a wrong branch here does not throw, it silently produces an empty array,
/// which for a fan-out migrator means the child rows are simply never created with no error
/// anywhere in the pipeline.
/// </summary>
public sealed class SourceRowExtensionsArrayTests
{
    [Fact]
    public void GetStringArray_ColumnMissing_ReturnsEmpty()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetStringArray("channels").Should().BeEmpty();
    }

    [Fact]
    public void GetStringArray_NativeArrayShape_ReturnsElements()
    {
        string[] channels = { "email", "sms" };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["channels"] = channels });

        row.GetStringArray("channels").Should().BeEquivalentTo(channels);
    }

    [Fact]
    public void GetStringArray_JsonTextShape_Deserializes()
    {
        string[] expected = { "email", "sms" };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["channels"] = "[\"email\",\"sms\"]" });

        row.GetStringArray("channels").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetStringArray_EmptyStringColumn_ReturnsEmpty()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["channels"] = string.Empty });

        row.GetStringArray("channels").Should().BeEmpty();
    }

    [Fact]
    public void GetInt32Array_ColumnMissing_ReturnsEmpty()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetInt32Array("ids").Should().BeEmpty();
    }

    [Fact]
    public void GetInt32Array_NativeArrayShape_ReturnsElements()
    {
        int[] ids = { 1, 2, 3 };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["ids"] = ids });

        row.GetInt32Array("ids").Should().BeEquivalentTo(ids);
    }

    [Fact]
    public void GetInt32Array_JsonTextShape_Deserializes()
    {
        int[] expected = { 1, 2, 3 };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["ids"] = "[1,2,3]" });

        row.GetInt32Array("ids").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetFloatArray_ColumnMissing_ReturnsEmpty()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetFloatArray("embedding").Should().BeEmpty();
    }

    [Fact]
    public void GetFloatArray_NativeFloatArrayShape_ReturnsElements()
    {
        float[] embedding = { 0.1f, 0.2f };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["embedding"] = embedding });

        row.GetFloatArray("embedding").Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public void GetFloatArray_NativeDoubleArrayShape_Converts()
    {
        double[] embedding = { 0.1, 0.2 };
        float[] expected = { 0.1f, 0.2f };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["embedding"] = embedding });

        row.GetFloatArray("embedding").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetFloatArray_JsonTextShape_Deserializes()
    {
        float[] expected = { 0.1f, 0.2f };
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["embedding"] = "[0.1,0.2]" });

        row.GetFloatArray("embedding").Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetInt32_ProviderNativeShortShape_ConvertsViaGenericPath()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = (short)7 });

        row.GetInt32("id").Should().Be(7);
    }

    [Fact]
    public void GetBoolean_ProviderNativeIntShape_ConvertsViaGenericPath()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["is_active"] = 1 });

        row.GetBoolean("is_active").Should().BeTrue();
    }

    [Fact]
    public void GetNullableFloat_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["score"] = null });

        row.GetNullableFloat("score").Should().BeNull();
    }

    [Fact]
    public void GetNullableFloat_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["score"] = 3.5 });

        row.GetNullableFloat("score").Should().Be(3.5f);
    }
}
