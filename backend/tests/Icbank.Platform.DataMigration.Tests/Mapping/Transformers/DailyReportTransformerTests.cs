using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>Unit tests for <see cref="DailyReportTransformer"/>.</summary>
public sealed class DailyReportTransformerTests
{
    [Fact]
    public void Transform_FullRow_MapsAllFields()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>
        {
            ["id"] = 7,
            ["report_date"] = new DateTime(2024, 3, 15),
            ["report_data"] = "{\"summary\":\"ok\",\"count\":3}",
            ["created_at"] = new DateTime(2024, 3, 15, 8, 0, 0),
        });

        MappedDailyReport mapped = DailyReportTransformer.Transform(row);

        mapped.SourceId.Should().Be(7);
        mapped.ReportDate.Should().Be(new DateOnly(2024, 3, 15));
        mapped.ReportDataJson.Should().Be("{\"summary\":\"ok\",\"count\":3}");
        mapped.CreatedAtUtc.Should().Be(new DateTime(2024, 3, 15, 8, 0, 0));
    }

    [Fact]
    public void Transform_ReportDataNull_FallsBackToEmptyJsonObject()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>
        {
            ["id"] = 8,
            ["report_date"] = new DateTime(2024, 3, 16),
            ["report_data"] = null,
            ["created_at"] = new DateTime(2024, 3, 16, 8, 0, 0),
        });

        MappedDailyReport mapped = DailyReportTransformer.Transform(row);

        mapped.ReportDataJson.Should().Be("{}");
    }

    [Fact]
    public void Transform_CreatedAtNull_Throws()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>
        {
            ["id"] = 9,
            ["report_date"] = new DateTime(2024, 3, 17),
            ["report_data"] = "{}",
            ["created_at"] = null,
        });

        Action act = () => DailyReportTransformer.Transform(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Transform_ReportDateAsIsoString_Parses()
    {
        SourceRow row = SourceRowFixture.Build(new Dictionary<string, object?>
        {
            ["id"] = 10,
            ["report_date"] = "2024-03-18",
            ["report_data"] = "{}",
            ["created_at"] = new DateTime(2024, 3, 18, 8, 0, 0),
        });

        MappedDailyReport mapped = DailyReportTransformer.Transform(row);

        mapped.ReportDate.Should().Be(new DateOnly(2024, 3, 18));
    }
}
