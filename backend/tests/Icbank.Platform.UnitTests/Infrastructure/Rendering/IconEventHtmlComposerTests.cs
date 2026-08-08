using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Designs;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Rendering;

/// <summary>Verifies <see cref="IconEventHtmlComposer"/> produces a correctly-sized, safe document for every preset and layout.</summary>
public sealed class IconEventHtmlComposerTests
{
    private readonly IconEventHtmlComposer _composer = new();

    public static TheoryData<IconEventSizePreset, IconEventLayoutType> EveryCombination
    {
        get
        {
            var data = new TheoryData<IconEventSizePreset, IconEventLayoutType>();
            foreach (IconEventSizePreset size in Enum.GetValues<IconEventSizePreset>())
            {
                foreach (IconEventLayoutType layout in Enum.GetValues<IconEventLayoutType>())
                {
                    data.Add(size, layout);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryCombination))]
    public void Render_EveryPresetAndLayout_SetsTheCanvasToThePresetDimensions(IconEventSizePreset size, IconEventLayoutType layout)
    {
        var html = _composer.Render(Input(size, layout));

        (var width, var height) = IconEventSizeCatalog.Dimensions(size);
        html.Should().Contain(FormattableString.Invariant($"width:{width}px;height:{height}px"));
    }

    [Theory]
    [InlineData(IconEventSizePreset.WebSmall)]
    [InlineData(IconEventSizePreset.WebMini)]
    public void Render_MiniPresets_OmitTheLogoAndDepartmentChrome(IconEventSizePreset size)
    {
        var html = _composer.Render(Input(size, IconEventLayoutType.Grid));

        html.Should().NotContain("الإدارة العامة للاتصال المؤسسي");
    }

    [Fact]
    public void Render_LargePresets_KeepTheLogoAndDepartmentChrome()
    {
        var html = _composer.Render(Input(IconEventSizePreset.DesktopHd, IconEventLayoutType.Grid));

        html.Should().Contain("الإدارة العامة للاتصال المؤسسي");
    }

    [Fact]
    public void Render_HeadlineContainingMarkup_IsEncoded()
    {
        IconEventInput input = Input(IconEventSizePreset.DesktopHd, IconEventLayoutType.Typography);
        input.Headline = "<script>alert(1)</script>";

        var html = _composer.Render(input);

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Fact]
    public void Render_GridPlates_ScaleWithTheCanvasRatherThanStayingFixed()
    {
        // Regression guard: the plate geometry was authored at 2000x1125 and emitted verbatim, which
        // pushed a 200px tile onto a 479px-tall card and clipped the meta row off the bottom edge.
        var large = _composer.Render(Input(IconEventSizePreset.DesktopHd, IconEventLayoutType.Grid));
        var small = _composer.Render(Input(IconEventSizePreset.WebMini, IconEventLayoutType.Grid));

        large.Should().Contain(Tile(864));
        small.Should().Contain(Tile(479));
    }

    [Fact]
    public void SizeCatalog_ArabicLabels_UseLatinDigits()
    {
        // The size labels are rendered straight into the designer UI, which is held to the same
        // Latin-digit rule as the rest of the shipped frontend.
        var arabicIndicDigits = new Regex("[\u0660-\u0669]", RegexOptions.None, TimeSpan.FromSeconds(1));

        IEnumerable<string> offenders = Enum.GetValues<IconEventSizePreset>()
            .Select(IconEventSizeCatalog.Resolve)
            .Select(preset => preset.ArabicLabel)
            .Where(label => arabicIndicDigits.IsMatch(label));

        offenders.Should().BeEmpty();
    }

    private static string Tile(int canvasHeight)
    {
        var side = (int)Math.Round(200 * (canvasHeight / 1125.0), MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture, $"width:{side}px;height:{side}px");
    }

    private static IconEventInput Input(IconEventSizePreset size, IconEventLayoutType layout) => new()
    {
        Headline = "ملتقى الامتثال والمنافسة العادلة",
        Subtitle = "النسخة الثانية — تمكين المنشآت من فهم نظام المنافسة",
        Department = "الإدارة العامة للاتصال المؤسسي",
        Hashtag = "#هيئة_المنافسة",
        ContactEmail = "info@gac.gov.sa",
        ContactPhone = "920000000",
        Date = "١٠ أغسطس ٢٠٢٦",
        Time = "١٠:٠٠ صباحاً",
        Location = "الرياض",
        MainIcon = "users",
        SupportingIcons = new List<string> { "calendar", "clock", "map-pin" },
        Stats = new List<IconEventStat> { new("users", "135+", "مشارك") },
        Layout = layout,
        Size = size,
    };
}
