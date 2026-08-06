using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// <see cref="JsonColumnReader"/> sits on every destination entity that stores a nested jsonb
/// payload (design templates, media reports, GAC publications, ...). Before this test class it
/// was measured at only 20% line coverage: most of its branches -- the <see cref="JsonElement"/>
/// shape (what a live Npgsql connection actually returns), the null/"null"-string shapes, and the
/// object-fallback branch -- had never executed under any test. A silently wrong branch here does
/// not throw; it deserializes to an empty/default object and writes that to the destination,
/// which is exactly the "reports success while migrating nothing (of the payload)" failure shape
/// this whole engagement exists to catch.
/// </summary>
public sealed class JsonColumnReaderTests
{
    [Fact]
    public void ReadObject_ColumnMissing_ReturnsNull()
    {
        var row = new SourceRow(new Dictionary<string, object?>());

        SamplePayload? result = row.ReadObject<SamplePayload>("payload");

        result.Should().BeNull();
    }

    [Fact]
    public void ReadObject_JsonStringValue_DeserializesCaseInsensitively()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = "{\"name\":\"x\",\"count\":3}" });

        SamplePayload? result = row.ReadObject<SamplePayload>("payload");

        result.Should().BeEquivalentTo(new SamplePayload("x", 3));
    }

    [Fact]
    public void ReadObject_LiteralNullString_ReturnsNull()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = "null" });

        row.ReadObject<SamplePayload>("payload").Should().BeNull();
    }

    [Fact]
    public void ReadObject_WhitespaceString_ReturnsNull()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = "   " });

        row.ReadObject<SamplePayload>("payload").Should().BeNull();
    }

    [Fact]
    public void ReadObject_JsonElementShape_DeserializesLikeALiveNpgsqlColumn()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"live\",\"count\":7}");
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = doc.RootElement.Clone() });

        SamplePayload? result = row.ReadObject<SamplePayload>("payload");

        result.Should().BeEquivalentTo(new SamplePayload("live", 7));
    }

    [Fact]
    public void ReadObject_JsonElementNullKind_ReturnsNull()
    {
        using var doc = JsonDocument.Parse("null");
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = doc.RootElement.Clone() });

        row.ReadObject<SamplePayload>("payload").Should().BeNull();
    }

    [Fact]
    public void ReadObjectList_ColumnMissing_ReturnsEmptyListNotNull()
    {
        var row = new SourceRow(new Dictionary<string, object?>());

        List<SamplePayload> result = row.ReadObjectList<SamplePayload>("items");

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ReadObjectList_JsonArrayString_DeserializesAllElements()
    {
        var row = new SourceRow(new Dictionary<string, object?>
        {
            ["items"] = "[{\"name\":\"a\",\"count\":1},{\"name\":\"b\",\"count\":2}]",
        });

        List<SamplePayload> result = row.ReadObjectList<SamplePayload>("items");

        result.Should().BeEquivalentTo(new[] { new SamplePayload("a", 1), new SamplePayload("b", 2) });
    }

    [Fact]
    public void ReadObjectList_NumericValueForStringProperty_PreservesItsTextualValue()
    {
        var row = new SourceRow(new Dictionary<string, object?>
        {
            ["items"] = """[{"fontWeight":700}]""",
        });

        List<FontPayload> result = row.ReadObjectList<FontPayload>("items");

        result.Should().ContainSingle().Which.FontWeight.Should().Be("700");
    }

    [Fact]
    public void ReadObject_NullValueForNonNullableInt_DefaultsToZero()
    {
        var row = new SourceRow(new Dictionary<string, object?>
        {
            ["payload"] = """{"name":"x","count":null}""",
        });

        SamplePayload? result = row.ReadObject<SamplePayload>("payload");

        result.Should().BeEquivalentTo(new SamplePayload("x", 0));
    }

    [Fact]
    public void ReadObjectList_JsonElementArrayShape_DeserializesLikeALiveNpgsqlColumn()
    {
        using var doc = JsonDocument.Parse("[{\"name\":\"a\",\"count\":1}]");
        var row = new SourceRow(new Dictionary<string, object?> { ["items"] = doc.RootElement.Clone() });

        List<SamplePayload> result = row.ReadObjectList<SamplePayload>("items");

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new SamplePayload("a", 1));
    }

    [Fact]
    public void ReadObjectList_NullColumn_ReturnsEmptyList()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["items"] = null });

        row.ReadObjectList<SamplePayload>("items").Should().BeEmpty();
    }

    [Fact]
    public void ReadRawJsonText_ColumnMissing_ReturnsFallback()
    {
        var row = new SourceRow(new Dictionary<string, object?>());

        row.ReadRawJsonText("payload", "{}").Should().Be("{}");
    }

    [Fact]
    public void ReadRawJsonText_StringValue_ReturnsVerbatim()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = "{\"a\":1}" });

        row.ReadRawJsonText("payload", "{}").Should().Be("{\"a\":1}");
    }

    [Fact]
    public void ReadRawJsonText_JsonElementShape_ReturnsCanonicalRawText()
    {
        using var doc = JsonDocument.Parse("{\"a\":1}");
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = doc.RootElement.Clone() });

        row.ReadRawJsonText("payload", "{}").Should().Be("{\"a\":1}");
    }

    [Fact]
    public void ReadRawJsonText_LiteralNullString_ReturnsFallback()
    {
        var row = new SourceRow(new Dictionary<string, object?> { ["payload"] = "null" });

        row.ReadRawJsonText("payload", "fallback").Should().Be("fallback");
    }

    private sealed record SamplePayload(string Name, int Count);

    private sealed class FontPayload
    {
        public string? FontWeight { get; init; }
    }
}
