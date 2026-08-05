using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.InternationalDays;
using Icbank.Platform.Application.InternationalDays.Commands;
using Icbank.Platform.Domain.InternationalDays;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.InternationalDays;

/// <summary>
/// Verifies <see cref="SaveInternationalDayCommandHandler"/> closes DEFECT-LOG.md DATA-05: every
/// entity mutation across the day/theme/activation/design-sample/source tables happens before a
/// single <see cref="IApplicationDbContext.SaveChangesAsync"/> call, and an audit-log entry
/// follows (API-SURFACE.md §14).
/// </summary>
public sealed class SaveInternationalDayCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 21, 8, 0, 0, TimeSpan.Zero);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAsyncQueryExecutor _queryExecutor = Substitute.For<IAsyncQueryExecutor>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly SaveInternationalDayCommandHandler _handler;

    public SaveInternationalDayCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _dbContext.InternationalDays.Returns(Array.Empty<InternationalDay>().AsQueryable());
        _dbContext.DayYearlyThemes.Returns(Array.Empty<DayYearlyTheme>().AsQueryable());
        _dbContext.DayActivations.Returns(Array.Empty<DayActivation>().AsQueryable());
        _handler = new SaveInternationalDayCommandHandler(_dbContext, _queryExecutor, _dateTimeProvider, _auditLogService);
    }

    [Fact]
    public async Task Handle_NoExistingDay_InsertsNewDayAndReturnsSuccess()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        var command = new SaveInternationalDayCommand(3, MinimalData(), "توعوي");

        Result<SaveInternationalDayResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Day.DayNameAr.Should().Be("يوم المرأة");
        result.Value.Day.Category.Should().Be("توعوي");
        _dbContext.Received(1).Add(Arg.Is<InternationalDay>(d => d.DayNameAr == "يوم المرأة" && d.LastSearchedAt == FixedNow));
    }

    [Fact]
    public async Task Handle_ExistingDay_UpdatesInPlaceWithoutAddingNewDay()
    {
        var existing = new InternationalDay { DayNameAr = "يوم المرأة", Category = "old-category" };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(existing));
        var command = new SaveInternationalDayCommand(3, MinimalData(), Category: null);

        Result<SaveInternationalDayResultDto> result = await _handler.Handle(command, CancellationToken.None);

        existing.DayNameEn.Should().Be("Women's Day");
        existing.Category.Should().Be("old-category", "a null request category must not overwrite the existing one");
        existing.LastSearchedAt.Should().Be(FixedNow);
        _dbContext.DidNotReceive().Add(Arg.Any<InternationalDay>());
        result.Value!.Day.Category.Should().Be("old-category");
    }

    [Fact]
    public async Task Handle_ThemeProvidedNoExistingTheme_AddsNewYearlyTheme()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<DayYearlyTheme>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DayYearlyTheme?>(null));
        DaySearchResultDto data = MinimalData() with { CurrentThemeAr = "معاً", CurrentThemeEn = "Together" };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<DayYearlyTheme>(t => t.ThemeAr == "معاً" && t.ThemeEn == "Together" && t.Year == 2026));
    }

    [Fact]
    public async Task Handle_ThemeMissing_DoesNotQueryOrAddTheme()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        var command = new SaveInternationalDayCommand(3, MinimalData(), null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.DidNotReceive().Add(Arg.Any<DayYearlyTheme>());
    }

    [Fact]
    public async Task Handle_ActivationAlreadyExists_SkipsDuplicateInsert()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        _queryExecutor.AnyAsync(Arg.Any<IQueryable<DayActivation>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        DaySearchActivationDto[] activations = new[] { new DaySearchActivationDto("وزارة الصحة", "government", "campaign", "twitter", "desc", null, "SA", 2026) };
        DaySearchResultDto data = MinimalData() with { Activations = activations };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.DidNotReceive().Add(Arg.Any<DayActivation>());
    }

    [Fact]
    public async Task Handle_NewActivationWithSourceUrl_AddsVerifiedActivation()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        _queryExecutor.AnyAsync(Arg.Any<IQueryable<DayActivation>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        DaySearchActivationDto[] activations = new[] { new DaySearchActivationDto("وزارة الصحة", "government", "campaign", "twitter", "desc", "https://example.com", "SA", 2026) };
        DaySearchResultDto data = MinimalData() with { Activations = activations };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<DayActivation>(a => a.EntityName == "وزارة الصحة" && a.Verified && a.Year == 2026));
    }

    [Fact]
    public async Task Handle_DesignSampleWithoutEntityName_IsSkipped()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        DaySearchDesignSampleDto[] samples = new[] { new DaySearchDesignSampleDto(null, "private", "instagram", "desc", null, null, "SA", 2026) };
        DaySearchResultDto data = MinimalData() with { DesignSamples = samples };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.DidNotReceive().Add(Arg.Any<DayActivation>());
    }

    [Fact]
    public async Task Handle_NewDesignSample_AddsActivationTaggedWithDesignSampleType()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        _queryExecutor.AnyAsync(Arg.Any<IQueryable<DayActivation>>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        DaySearchDesignSampleDto[] samples = new[] { new DaySearchDesignSampleDto("هيئة", "government", "instagram", "desc", "https://page.example", null, "SA", 2026) };
        DaySearchResultDto data = MinimalData() with { DesignSamples = samples };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<DayActivation>(a => a.EntityName == "هيئة" && a.ActivationType == "تصميم بصري" && a.Verified));
    }

    [Fact]
    public async Task Handle_SourceWithoutUrl_IsSkipped()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        DaySearchSourceDto[] sources = new[] { new DaySearchSourceDto(null, "title", "publisher") };
        DaySearchResultDto data = MinimalData() with { Sources = sources };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.DidNotReceive().Add(Arg.Any<IntlDaySource>());
    }

    [Fact]
    public async Task Handle_SourceWithUrl_IsAdded()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        DaySearchSourceDto[] sources = new[] { new DaySearchSourceDto("https://example.com", "title", "publisher") };
        DaySearchResultDto data = MinimalData() with { Sources = sources };
        var command = new SaveInternationalDayCommand(3, data, null);

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Is<IntlDaySource>(s => s.SourceUrl == "https://example.com" && s.RelatedTable == "international_days"));
    }

    [Fact]
    public async Task Handle_ValidCommand_WritesAuditLogAfterSaveChanges()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<InternationalDay>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<InternationalDay?>(null));
        var command = new SaveInternationalDayCommand(9, MinimalData(), null);

        await _handler.Handle(command, CancellationToken.None);

        Received.InOrder(() =>
        {
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
            _auditLogService.RecordAsync(
                9,
                "international_day.save",
                "InternationalDay",
                Arg.Any<string>(),
                Arg.Is<object?>(before => before == null),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>());
        });
    }

    private static DaySearchResultDto MinimalData(string dayNameAr = "يوم المرأة") => new(
        dayNameAr, "Women's Day", "8 مارس", null, null, null, null, null, null, null, null, null, null, null);
}
