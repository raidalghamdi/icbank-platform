using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// Exhaustive unit tests for <see cref="SourceRowExtensions"/> -- the lowest layer every table
/// transformer builds on. Every accessor is tested for present, absent (SQL NULL/missing
/// column), and provider-shape-mismatch inputs, since a live Npgsql connection can return
/// int/long/other boxed CLR types depending on the Postgres column type.
/// </summary>
public sealed class SourceRowExtensionsTests
{
    private static readonly string[] ExpectedIdAndEmailColumnNames = { "id", "email" };

    [Fact]
    public void GetInt32_ColumnPresentAsInt_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = 42 });

        row.GetInt32("id").Should().Be(42);
    }

    [Fact]
    public void GetInt32_ColumnPresentAsLong_ConvertsDown()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = 42L });

        row.GetInt32("id").Should().Be(42);
    }

    [Fact]
    public void GetInt32_ColumnMissing_Throws()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        Action act = () => row.GetInt32("id");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetInt32_ColumnNull_Throws()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = null });

        Action act = () => row.GetInt32("id");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetNullableInt32_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["failed_attempts"] = null });

        row.GetNullableInt32("failed_attempts").Should().BeNull();
    }

    [Fact]
    public void GetNullableInt32_ColumnMissing_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetNullableInt32("failed_attempts").Should().BeNull();
    }

    [Fact]
    public void GetNullableInt32_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["failed_attempts"] = 3 });

        row.GetNullableInt32("failed_attempts").Should().Be(3);
    }

    [Fact]
    public void GetString_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["email"] = "a@example.com" });

        row.GetString("email").Should().Be("a@example.com");
    }

    [Fact]
    public void GetString_ColumnNull_ReturnsEmptyString()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["email"] = null });

        row.GetString("email").Should().BeEmpty();
    }

    [Fact]
    public void GetString_ColumnMissing_ReturnsEmptyString()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetString("email").Should().BeEmpty();
    }

    [Fact]
    public void GetNullableString_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["title"] = null });

        row.GetNullableString("title").Should().BeNull();
    }

    [Fact]
    public void GetNullableString_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["title"] = "Manager" });

        row.GetNullableString("title").Should().Be("Manager");
    }

    [Fact]
    public void GetBoolean_ColumnNull_ReturnsFalse()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["is_active"] = null });

        row.GetBoolean("is_active").Should().BeFalse();
    }

    [Fact]
    public void GetBoolean_ColumnMissing_ReturnsFalse()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetBoolean("is_active").Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetBoolean_ColumnPresent_ReturnsValue(bool value)
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["is_active"] = value });

        row.GetBoolean("is_active").Should().Be(value);
    }

    [Fact]
    public void GetNullableBoolean_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["auto_generate"] = null });

        row.GetNullableBoolean("auto_generate").Should().BeNull();
    }

    [Fact]
    public void GetNullableBoolean_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["auto_generate"] = true });

        row.GetNullableBoolean("auto_generate").Should().BeTrue();
    }

    [Fact]
    public void GetRawTimestamp_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["last_login"] = null });

        row.GetRawTimestamp("last_login").Should().BeNull();
    }

    [Fact]
    public void GetRawTimestamp_ColumnMissing_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        row.GetRawTimestamp("last_login").Should().BeNull();
    }

    [Fact]
    public void GetRawTimestamp_ColumnPresentAsDateTime_ReturnsAsIsWithNoConversion()
    {
        var raw = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Unspecified);
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["created_at"] = raw });

        row.GetRawTimestamp("created_at").Should().Be(raw);
    }

    [Fact]
    public void GetRawTimestamp_ColumnPresentAsIsoString_Parses()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["created_at"] = "2024-03-15T10:30:00" });

        row.GetRawTimestamp("created_at").Should().Be(new DateTime(2024, 3, 15, 10, 30, 0));
    }

    [Fact]
    public void GetNullableDecimal_ColumnNull_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["amount"] = null });

        row.GetNullableDecimal("amount").Should().BeNull();
    }

    [Fact]
    public void GetNullableDecimal_ColumnPresent_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["amount"] = 12.5m });

        row.GetNullableDecimal("amount").Should().Be(12.5m);
    }

    [Fact]
    public void GetDateOnly_ColumnPresentAsDateTime_ConvertsDropsTimeComponent()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["report_date"] = new DateTime(2024, 3, 15, 10, 30, 0) });

        row.GetDateOnly("report_date").Should().Be(new DateOnly(2024, 3, 15));
    }

    [Fact]
    public void GetDateOnly_ColumnPresentAsDateOnly_ReturnsValue()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["report_date"] = new DateOnly(2024, 3, 15) });

        row.GetDateOnly("report_date").Should().Be(new DateOnly(2024, 3, 15));
    }

    [Fact]
    public void GetDateOnly_ColumnPresentAsIsoString_Parses()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["report_date"] = "2024-03-15" });

        row.GetDateOnly("report_date").Should().Be(new DateOnly(2024, 3, 15));
    }

    [Fact]
    public void GetDateOnly_ColumnNull_Throws()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["report_date"] = null });

        Action act = () => row.GetDateOnly("report_date");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetDateOnly_ColumnMissing_Throws()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>());

        Action act = () => row.GetDateOnly("report_date");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ColumnNames_ReflectsProvidedColumns()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = 1, ["email"] = "a@b.com" });

        row.ColumnNames.Should().BeEquivalentTo(ExpectedIdAndEmailColumnNames);
    }

    [Fact]
    public void Indexer_UnknownColumn_ReturnsNull()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?> { ["id"] = 1 });

        row["does_not_exist"].Should().BeNull();
    }
}
