using System.Text;
using FluentAssertions;
using Icbank.Platform.Application.Admin.Queries;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Admin;

/// <summary>
/// Verifies <see cref="ActivityLogCsvWriter"/>: the UTF-8 BOM is present as real leading bytes,
/// Arabic text round-trips exactly, CSV-injection-triggering values are neutralized, and internal
/// quotes are doubled per RFC 4180.
/// </summary>
public sealed class ActivityLogCsvWriterTests
{
    [Fact]
    public async Task WriteAsync_EmitsUtf8BomAsLeadingBytes()
    {
        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(Array.Empty<ActivityLogExportRow>(), stream, CancellationToken.None);

        var bytes = stream.ToArray();
        bytes.Take(3).Should().BeEquivalentTo(new byte[] { 0xEF, 0xBB, 0xBF }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task WriteAsync_ArabicUserName_RoundTripsExactlyAfterBom()
    {
        var row = new ActivityLogExportRow(1, "مدير النظام", "admin@test.local", "login_success", "user", "1", "127.0.0.1", DateTime.UtcNow);

        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(new[] { row }, stream, CancellationToken.None);

        var text = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
        text.Should().Contain("مدير النظام");
        text.Should().Contain("المستخدم"); // header
    }

    [Theory]
    [InlineData("=cmd|'/c calc'!A0")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1)")]
    public async Task WriteAsync_FormulaInjectionPrefix_IsNeutralizedWithLeadingApostrophe(string dangerousAction)
    {
        var row = new ActivityLogExportRow(1, "User", "user@test.local", dangerousAction, null, null, null, DateTime.UtcNow);

        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(new[] { row }, stream, CancellationToken.None);

        var text = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
        text.Should().Contain("\"'" + dangerousAction);
    }

    [Fact]
    public async Task WriteAsync_ValueWithEmbeddedQuote_DoublesTheQuote()
    {
        var row = new ActivityLogExportRow(1, "Say \"hi\"", "user@test.local", "login_success", null, null, null, DateTime.UtcNow);

        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(new[] { row }, stream, CancellationToken.None);

        var text = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
        text.Should().Contain("Say \"\"hi\"\"");
    }

    [Fact]
    public async Task WriteAsync_MissingUserFields_RenderEmDashPlaceholder()
    {
        var row = new ActivityLogExportRow(1, null, null, "login_failed", null, null, null, DateTime.UtcNow);

        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(new[] { row }, stream, CancellationToken.None);

        var text = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
        text.Should().Contain("\"—\"");
    }

    [Fact]
    public async Task WriteAsync_HeaderRow_MatchesNodeOriginalColumnOrder()
    {
        using var stream = new MemoryStream();
        await ActivityLogCsvWriter.WriteAsync(Array.Empty<ActivityLogExportRow>(), stream, CancellationToken.None);

        var text = Encoding.UTF8.GetString(stream.ToArray().Skip(3).ToArray());
        var headerLine = text.Split('\n')[0];
        headerLine.Should().Be("\"#\",\"المستخدم\",\"البريد\",\"العملية\",\"النوع\",\"المعرف\",\"IP\",\"التاريخ\"");
    }
}
