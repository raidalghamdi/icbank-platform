using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.Application.Reports;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Verifies the ported n8n field-remapping rule (BUSINESS-RULES.md §6).</summary>
public sealed class N8NPayloadNormalizerTests
{
    [Fact]
    public void Normalize_RemapsSnakeCaseFieldsAndStampsProvenance()
    {
        var raw = """{"report_date":"2026-08-05","overdue_projects":[{"a":1}],"due_soon_projects":[{"b":2}],"target_initiatives":[{"c":3}]}""";
        var receivedAt = new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

        var normalized = N8NPayloadNormalizer.Normalize(raw, receivedAt);
        using var document = JsonDocument.Parse(normalized);
        JsonElement root = document.RootElement;

        root.TryGetProperty("overdueProjects", out _).Should().BeTrue();
        root.TryGetProperty("dueSoon", out _).Should().BeTrue();
        root.TryGetProperty("initiatives", out _).Should().BeTrue();
        root.GetProperty("_source").GetString().Should().Be("n8n");
        root.TryGetProperty("report_date", out _).Should().BeFalse("the original date key must be removed from the stored payload");
        root.TryGetProperty("reportDate", out _).Should().BeFalse();
    }

    [Fact]
    public void Normalize_KpisAsNonObject_IsNotPassedThrough()
    {
        var raw = """{"reportDate":"2026-08-05","kpis":"not-an-object"}""";

        var normalized = N8NPayloadNormalizer.Normalize(raw, DateTimeOffset.UtcNow);
        using var document = JsonDocument.Parse(normalized);

        document.RootElement.GetProperty("kpis").ValueKind.Should().Be(JsonValueKind.String, "non-object kpis must pass through unchanged, not be dropped or coerced");
    }

    [Fact]
    public void ExtractReportDate_PrefersSnakeCaseOverCamelCase()
    {
        var raw = """{"report_date":"2026-08-05","reportDate":"2026-09-01"}""";

        var extracted = N8NPayloadNormalizer.ExtractReportDate(raw);

        extracted.Should().Be("2026-08-05");
    }

    [Fact]
    public void ExtractReportDate_NeitherKeyPresent_ReturnsNull()
    {
        var raw = """{"title":"x"}""";

        var extracted = N8NPayloadNormalizer.ExtractReportDate(raw);

        extracted.Should().BeNull();
    }
}
