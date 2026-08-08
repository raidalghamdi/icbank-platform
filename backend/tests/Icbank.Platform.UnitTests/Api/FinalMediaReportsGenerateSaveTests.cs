using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Icbank.Platform.Api.Controllers;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Api;

/// <summary>
/// Guards the generate-then-save wiring on <c>POST /final-media-reports/generate</c>.
/// </summary>
/// <remarks>
/// The endpoint used to return <c>saved = null</c> unconditionally, with no code path that could
/// ever set it. Nothing failed loudly: the frontend's "saved" branch was simply dead, every
/// generated report stayed a draft with no server id, the archive stayed empty, and the PDF,
/// email and exec-summary buttons all built <c>/final-media-reports/undefined/...</c> and came
/// back as a bare 404 that read like a server fault. These tests fail if the save is removed
/// again, and if a failed save is ever allowed to swallow the generated draft.
/// </remarks>
public sealed class FinalMediaReportsGenerateSaveTests
{
    private const int ActorUserId = 2;

    private const string PeriodLabel = "الأسبوع 32 - 2026";

    [Fact]
    public async Task GenerateAsync_PersistsTheDraftAndReturnsTheSavedReport()
    {
        ISender sender = Substitute.For<ISender>();
        StubGenerate(sender, Draft());
        FinalMediaReportDto report = SavedReport(id: 41);
        sender.Send(Arg.Any<CreateFinalMediaReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<FinalMediaReportDto>.Success(report));

        ActionResult<GenerateFinalMediaReportResultDto> response = await Controller(sender).GenerateAsync(Request());

        var payload = OkPayload(response);
        ReadSaved(payload).Should().BeSameAs(report);
        await sender.Received(1).Send(
            Arg.Is<CreateFinalMediaReportCommand>(c =>
                c.ActorUserId == ActorUserId && c.PeriodLabel == PeriodLabel),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WhenTheSaveFails_StillReturnsTheDraft()
    {
        ISender sender = Substitute.For<ISender>();
        FinalReportDraftDto draft = Draft();
        StubGenerate(sender, draft);
        sender.Send(Arg.Any<CreateFinalMediaReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<FinalMediaReportDto>.Failure("save exploded"));

        ActionResult<GenerateFinalMediaReportResultDto> response = await Controller(sender).GenerateAsync(Request());

        var payload = OkPayload(response);
        ReadSaved(payload).Should().BeNull();
        ReadDraft(payload).Should().BeSameAs(draft);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotObserveTheClientConnection()
    {
        // A phone on mobile data drops the idle connection long before this ~100-second call
        // answers. While the endpoint took HttpContext.RequestAborted, that drop cancelled the
        // generation itself and a hundred seconds of paid AI work was thrown away with nothing
        // saved. The token reaching the handler must not be the aborted request token.
        ISender sender = Substitute.For<ISender>();
        StubGenerate(sender, Draft());
        sender.Send(Arg.Any<CreateFinalMediaReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<FinalMediaReportDto>.Success(SavedReport(id: 7)));

        FinalMediaReportsController controller = Controller(sender);
        using var aborted = new CancellationTokenSource();
        controller.ControllerContext.HttpContext.RequestAborted = aborted.Token;
        await aborted.CancelAsync();

        await controller.GenerateAsync(Request());

        await sender.Received(1).Send(
            Arg.Any<GenerateFinalMediaReportCommand>(),
            Arg.Is<CancellationToken>(t => !t.IsCancellationRequested));
    }

    private static void StubGenerate(ISender sender, FinalReportDraftDto draft) =>
        sender.Send(Arg.Any<GenerateFinalMediaReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<GenerateFinalMediaReportResultDto>.Success(
                new GenerateFinalMediaReportResultDto(draft, null)));

    private static GenerateFinalMediaReportRequest Request() =>
        new(PeriodLabel, "manager", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, null, null);

    // Only the draft's identity matters here -- the controller passes it straight through and
    // never reads its sections -- so the nested section DTOs stay unpopulated on purpose.
    private static FinalReportDraftDto Draft() =>
        new(PeriodLabel, "ملخص", null!, [], [], null!, null!, null!, [], [], [], [], "المنهجية", []);

    private static FinalMediaReportDto SavedReport(int id) =>
        new(id, $"R-{id}", "التقرير الإعلامي", "manager", PeriodLabel, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, "ملخص", null!, "final", 0, "sha", DateTimeOffset.UtcNow);

    private static FinalMediaReportsController Controller(ISender sender)
    {
        IHostApplicationLifetime lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ActorUserId.ToString(CultureInfo.InvariantCulture))], "test");
        return new FinalMediaReportsController(sender, lifetime)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
            },
        };
    }

    private static object OkPayload(ActionResult<GenerateFinalMediaReportResultDto> response)
    {
        OkObjectResult ok = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().NotBeNull().And.Subject!;
    }

    private static object? ReadSaved(object payload) =>
        payload.GetType().GetProperty("saved")!.GetValue(payload);

    private static object? ReadDraft(object payload) =>
        payload.GetType().GetProperty("draft")!.GetValue(payload);
}
