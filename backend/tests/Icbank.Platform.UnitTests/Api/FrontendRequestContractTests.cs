using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.Api.Controllers;
using Xunit;

namespace Icbank.Platform.UnitTests.Api;

/// <summary>
/// Locks the wire contract between <c>artifacts/internal-comms/index.html</c> and the request
/// records it posts to.
/// </summary>
/// <remarks>
/// Why this file exists: the frontend was written against the old Node API and kept posting
/// Node-era key names after the .NET port. Because every optional property is nullable and the API
/// binds with the default camelCase policy, an unrecognised key bound to <c>null</c> instead of
/// failing loudly, and the request only blew up later as an opaque 400 "Validation failed" with no
/// field named. Two live symptoms traced back here: the icon-event designer sending
/// <c>raw_data</c>/<c>event_type</c>, and the media-monitoring report generator omitting the
/// required <c>periodLabel</c> entirely. The JSON literals below are the exact bodies the frontend
/// now sends, so a future rename on either side fails a test instead of shipping a dead button.
/// </remarks>
public sealed class FrontendRequestContractTests
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void IconEventGenerate_RawModeBody_BindsRawData()
    {
        const string body = """
        {"department":"","hashtag":"","date":"","time":"","location":"","rawData":"ورشة عمل عن الامتثال بحضور 40 موظفاً"}
        """;

        GenerateIconEventDesignRequest? request =
            JsonSerializer.Deserialize<GenerateIconEventDesignRequest>(body, ApiJsonOptions);

        request.Should().NotBeNull();
        request!.RawData.Should().NotBeNullOrWhiteSpace();

        // The designer no longer picks a size before generating, so the key is absent and the
        // handler falls back to the preview preset.
        request.Size.Should().BeNull();
    }

    [Fact]
    public void IconEventGenerate_StructuredModeBody_BindsHeadlineAndEventType()
    {
        const string body = """
        {"department":"إدارة الاتصال","hashtag":"#هيئة_المنافسة","date":"2026-08-10","time":"10:00","location":"الرياض","headline":"ملتقى الامتثال","subtitle":"النسخة الثانية","eventType":"meeting"}
        """;

        GenerateIconEventDesignRequest? request =
            JsonSerializer.Deserialize<GenerateIconEventDesignRequest>(body, ApiJsonOptions);

        request.Should().NotBeNull();
        request!.Headline.Should().Be("ملتقى الامتثال");
        request.EventType.Should().Be("meeting");
        request.Size.Should().BeNull();
    }

    [Theory]
    [InlineData("raw_data")]
    [InlineData("event_type")]
    public void IconEventGenerate_SnakeCaseKey_DoesNotBind(string snakeCaseKey)
    {
        // Documents the failure mode rather than a desired behaviour: the API registers no
        // snake_case naming policy, so these keys are silently ignored. This is why the frontend
        // must send camelCase, and why an unknown key can never be trusted to surface an error.
        var body = $$"""
        {"{{snakeCaseKey}}":"قيمة كافية للتجاوز"}
        """;

        GenerateIconEventDesignRequest? request =
            JsonSerializer.Deserialize<GenerateIconEventDesignRequest>(body, ApiJsonOptions);

        request.Should().NotBeNull();
        request!.RawData.Should().BeNull();
        request.EventType.Should().BeNull();
    }

    [Fact]
    public void IconEventStudio_FrontendBody_BindsChosenVariantAndSizes()
    {
        const string body = """
        {"headline":"ملتقى الامتثال","subtitle":"النسخة الثانية","department":"إدارة الاتصال","hashtag":"#هيئة_المنافسة","contactEmail":"info@gac.gov.sa","contactPhone":"920000000","date":"2026-08-10","time":"10:00","location":"الرياض","mainIcon":"users","supportingIcons":["calendar","clock"],"stats":[{"icon":"users","value":"135+","label":"مشارك"}],"layout":"stats-hero","sizes":["uhd-4k","desktop-hd","web-standard"]}
        """;

        GenerateIconEventStudioRequest? request =
            JsonSerializer.Deserialize<GenerateIconEventStudioRequest>(body, ApiJsonOptions);

        request.Should().NotBeNull();
        request!.Headline.Should().Be("ملتقى الامتثال");
        request.Layout.Should().Be("stats-hero");
        request.Sizes.Should().BeEquivalentTo("uhd-4k", "desktop-hd", "web-standard");
        request.SupportingIcons.Should().HaveCount(2);
        request.Stats.Should().ContainSingle(s => s.Value == "135+");
        request.ContactEmail.Should().Be("info@gac.gov.sa");
    }

    [Fact]
    public void FinalMediaReportGenerate_FrontendBody_BindsRequiredPeriodFields()
    {
        const string body = """
        {"periodLabel":"آخر 7 أيام (2026-07-31 — 2026-08-07)","audience":"manager","dateFrom":"2026-07-31T15:05:00.000Z","dateTo":"2026-08-07T15:05:00.000Z","focusTopics":""}
        """;

        GenerateFinalMediaReportRequest? request =
            JsonSerializer.Deserialize<GenerateFinalMediaReportRequest>(body, ApiJsonOptions);

        request.Should().NotBeNull();
        request!.PeriodLabel.Should().NotBeNullOrWhiteSpace();
        request.DateTo.Should().BeAfter(request.DateFrom);
        request.Audience.Should().Be("manager");
    }
}
