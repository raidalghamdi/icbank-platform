using FluentAssertions;
using Icbank.Platform.Domain.Designs;
using Xunit;

namespace Icbank.Platform.UnitTests.Domain.Designs;

public sealed class IconEventContentPlannerTests
{
    private const string AttendanceNotice =
        "وفقًا للضوابط المعتمدة، يجوز للمدير التنفيذي طلب استثناء أحد الموظفين من الحسم من الأجر. " +
        "احرص على: * مراجعة سجلات الحضور والانصراف للموظفين بشكل شهري. " +
        "* رفع طلب الاستثناء إلى إدارة الموارد البشرية، متضمنًا المبررات اللازمة، عند الحاجة.";

    private static readonly string[] SuggestedIcons = { "users", "users", "not-real" };
    private static readonly string[] BulletTexts = { "لائحة الضوابط المعتمدة", "طلب استثناء" };

    [Fact]
    public void InlineBulletMarkers_BecomeSeparateItems()
    {
        IconEventTextStructure structure = IconEventTextStructureParser.Parse(AttendanceNotice);

        structure.Bullets.Should().HaveCount(2);
        structure.Bullets[0].Should().StartWith("مراجعة سجلات الحضور");
        structure.Bullets.Should().OnlyContain(bullet => !bullet.Contains('*', StringComparison.Ordinal));
    }

    [Fact]
    public void LineBulletMarkers_AreRecognisedAcrossStyles()
    {
        IconEventTextStructure structure = IconEventTextStructureParser.Parse("مقدمة\n- أولاً\n• ثانياً\n2) ثالثاً");

        structure.Bullets.Should().Equal("أولاً", "ثانياً", "ثالثاً");
        structure.Lead.Should().Be("مقدمة");
    }

    [Fact]
    public void LabelledLine_BecomesASection()
    {
        IconEventTextStructure structure = IconEventTextStructureParser.Parse("موعد رفع الطلب:\nقبل اليوم العاشر من كل شهر.");

        structure.Sections.Should().ContainSingle();
        structure.Sections[0].Label.Should().Be("موعد رفع الطلب");
        structure.Sections[0].Body.Should().Be("قبل اليوم العاشر من كل شهر.");
    }

    [Fact]
    public void ClosingLine_IsSeparatedFromTheLead()
    {
        IconEventTextStructure structure = IconEventTextStructureParser.Parse("مقدمة طويلة.\n* بند\nسطر ختامي قصير.");

        structure.ClosingNote.Should().Be("سطر ختامي قصير.");
    }

    [Fact]
    public void PlainProse_ProducesNoBullets()
    {
        IconEventTextStructure structure = IconEventTextStructureParser.Parse("إعلان بسيط بدون أي تعداد.");

        structure.IsStructured.Should().BeFalse();
        structure.Lead.Should().Be("إعلان بسيط بدون أي تعداد.");
    }

    [Fact]
    public void EmptyInput_IsHandled()
    {
        IconEventTextStructureParser.Parse(null).Should().BeSameAs(IconEventTextStructure.Empty);
        IconEventTextStructureParser.Parse("   ").Should().BeSameAs(IconEventTextStructure.Empty);
    }

    [Theory]
    [InlineData(IconEventSizePreset.Uhd4k)]
    [InlineData(IconEventSizePreset.DesktopHd)]
    [InlineData(IconEventSizePreset.WebStandard)]
    [InlineData(IconEventSizePreset.WebSmall)]
    [InlineData(IconEventSizePreset.WebMini)]
    public void EverySize_StaysInsideItsOwnBudget(IconEventSizePreset size)
    {
        IconEventContentPlan plan = IconEventContentPlanner.Plan(Input(size));
        var budget = IconEventContentBudget.Resolve(size);

        plan.Headline.Length.Should().BeLessThanOrEqualTo(budget.HeadlineChars);
        (plan.Lead?.Length ?? 0).Should().BeLessThanOrEqualTo(budget.LeadChars);
        plan.Bullets.Should().HaveCountLessThanOrEqualTo(budget.MaxBullets);
        plan.Bullets.Where(bullet => bullet.Text.Length > budget.BulletChars).Should().BeEmpty();
        plan.MetaChips.Should().HaveCountLessThanOrEqualTo(budget.MaxMetaChips);
    }

    [Fact]
    public void TheMiniCanvas_DropsTheList()
    {
        IconEventContentPlanner.Plan(Input(IconEventSizePreset.WebMini)).Bullets.Should().BeEmpty();
        IconEventContentPlanner.Plan(Input(IconEventSizePreset.Uhd4k)).Bullets.Should().NotBeEmpty();
    }

    [Fact]
    public void Trimming_NeverCutsAWordInHalf()
    {
        var trimmed = IconEventTextTrimmer.Trim("كلمة أخرى وكلمة ثالثة ورابعة", 14);

        trimmed.Should().NotBeNull();
        trimmed!.TrimEnd('…').Split(' ').Should().OnlyContain(word => "كلمة أخرى وكلمة ثالثة ورابعة".Contains(word, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryResolvedIcon_ExistsInTheCatalogue()
    {
        IconEventInput input = Input(IconEventSizePreset.Uhd4k);
        input.MainIcon = "not-a-real-icon";
        IconEventContentPlan plan = IconEventContentPlanner.Plan(input);

        IconLibrary.ValidNames.Should().Contain(plan.MainIcon);
        plan.SupportingIcons.Where(icon => !IconLibrary.ValidNames.Contains(icon)).Should().BeEmpty();
        plan.Bullets.Where(bullet => !IconLibrary.ValidNames.Contains(bullet.Icon)).Should().BeEmpty();
    }

    [Fact]
    public void AnUnknownIconName_IsReplacedSemanticallyRatherThanDecoratively()
    {
        var resolved = IconEventIconResolver.Resolve("not-a-real-icon", "مراجعة سجلات الحضور والانصراف الشهرية");

        resolved.Should().NotBe("sparkles");
        IconLibrary.ValidNames.Should().Contain(resolved);
    }

    [Fact]
    public void SupportingIcons_AreDistinct()
    {
        IReadOnlyList<string> icons = IconEventIconResolver.ResolveMany(
            SuggestedIcons,
            BulletTexts,
            3);

        icons.Should().HaveCount(3).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void AValidSuggestedIcon_IsHonoured() =>
        IconEventIconResolver.Resolve("shield", "أي نص").Should().Be("shield");

    private static IconEventInput Input(IconEventSizePreset size) => new()
    {
        Headline = "متابعتك تصنع الفرق",
        Subtitle = AttendanceNotice,
        Department = "الإدارة العامة للاتصال المؤسسي",
        ContactEmail = "staffrelations@gac.gov.sa",
        ContactPhone = "920000000",
        Date = "10/07/2026",
        Time = "10:00",
        Location = "الرياض",
        MainIcon = "users",
        Size = size,
        Layout = IconEventLayoutType.Split,
    };
}
