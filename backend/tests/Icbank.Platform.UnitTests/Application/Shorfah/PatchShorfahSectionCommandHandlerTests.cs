using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Shorfah.Commands;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Shorfah;

/// <summary>
/// Verifies <see cref="PatchShorfahSectionCommandHandler"/> routes every
/// <see cref="PatchShorfahSectionCommand.ContentHtml"/> write through <see cref="IHtmlSanitizer"/>
/// before it is stored on <see cref="ShorfahSection.ContentHtml"/> (closes SEC-11), and that a
/// sanitization-changed audit entry is written when -- and only when -- sanitization actually
/// altered the input.
/// </summary>
public sealed class PatchShorfahSectionCommandHandlerTests
{
    private const int ActorUserId = 42;
    private const int SectionId = 7;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IShorfahSectionAccessService _accessService = Substitute.For<IShorfahSectionAccessService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IHtmlSanitizer _htmlSanitizer = Substitute.For<IHtmlSanitizer>();
    private readonly PatchShorfahSectionCommandHandler _handler;

    public PatchShorfahSectionCommandHandlerTests()
    {
        _handler = new PatchShorfahSectionCommandHandler(
            _dbContext, _queryExecutor, _accessService, _dateTimeProvider, _auditLogService, _htmlSanitizer);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _accessService.IsAdminAsync(ActorUserId, Arg.Any<CancellationToken>()).Returns(false);
        _accessService.CanAccessSectionAsync(ActorUserId, SectionId, ShorfahSectionAccessTier.Contribute, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Fact]
    public async Task Handle_ContentHtmlProvided_AlwaysPassesThroughSanitizerBeforeStoring()
    {
        ShorfahSection section = SeedSection();
        const string rawHtml = "<p>محتوى</p><script>alert(1)</script>";
        _htmlSanitizer.Sanitize(rawHtml).Returns(new HtmlSanitizationResult("<p>محتوى</p>", WasModified: true));

        await _handler.Handle(
            new PatchShorfahSectionCommand(ActorUserId, SectionId, null, rawHtml, null, null, null, null, null, null, null),
            CancellationToken.None);

        _htmlSanitizer.Received(1).Sanitize(rawHtml);
        section.ContentHtml.Should().Be("<p>محتوى</p>");
        section.ContentHtml.Should().NotContain("<script");
    }

    [Fact]
    public async Task Handle_SanitizationChangesInput_WritesSanitizationAuditEntry()
    {
        SeedSection();
        const string rawHtml = "<img src=x onerror=alert(1)>";
        _htmlSanitizer.Sanitize(rawHtml).Returns(new HtmlSanitizationResult("<img src=\"x\">", WasModified: true));

        await _handler.Handle(
            new PatchShorfahSectionCommand(ActorUserId, SectionId, null, rawHtml, null, null, null, null, null, null, null),
            CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId,
            "shorfah_section.content_html.sanitized",
            "ShorfahSection",
            SectionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Arg.Any<object?>(),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SanitizationDoesNotChangeInput_DoesNotWriteSanitizationAuditEntry()
    {
        SeedSection();
        const string cleanHtml = "<p>محتوى نظيف بالفعل</p>";
        _htmlSanitizer.Sanitize(cleanHtml).Returns(new HtmlSanitizationResult(cleanHtml, WasModified: false));

        await _handler.Handle(
            new PatchShorfahSectionCommand(ActorUserId, SectionId, null, cleanHtml, null, null, null, null, null, null, null),
            CancellationToken.None);

        await _auditLogService.DidNotReceive().RecordAsync(
            Arg.Any<int>(),
            "shorfah_section.content_html.sanitized",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ContentHtmlNull_DoesNotInvokeSanitizer()
    {
        SeedSection();

        await _handler.Handle(
            new PatchShorfahSectionCommand(ActorUserId, SectionId, "some markdown", null, null, null, null, null, null, null, null),
            CancellationToken.None);

        _htmlSanitizer.DidNotReceiveWithAnyArgs().Sanitize(default!);
    }

    private ShorfahSection SeedSection()
    {
        var section = new ShorfahSection { Id = SectionId, TitleAr = "عنوان" };
        _dbContext.ShorfahSections.Returns(new[] { section }.AsQueryable());
        return section;
    }
}
