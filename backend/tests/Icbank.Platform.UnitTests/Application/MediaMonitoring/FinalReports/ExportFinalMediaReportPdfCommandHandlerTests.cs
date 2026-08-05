using System.Text;
using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="ExportFinalMediaReportPdfCommandHandler"/> renders the report's HTML via the shared builder and delegates to the PDF port.</summary>
public sealed class ExportFinalMediaReportPdfCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IFinalReportPdfRenderer _pdfRenderer = Substitute.For<IFinalReportPdfRenderer>();
    private readonly ExportFinalMediaReportPdfCommandHandler _handler;

    public ExportFinalMediaReportPdfCommandHandlerTests()
    {
        _handler = new ExportFinalMediaReportPdfCommandHandler(_dbContext, _queryExecutor, _pdfRenderer);
    }

    [Fact]
    public async Task Handle_ExistingReport_PassesEncodedHtmlToPdfRendererAndReturnsBytes()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        var expectedBytes = Encoding.UTF8.GetBytes("%PDF-fake");
        _pdfRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expectedBytes);

        Result<byte[]> result = await _handler.Handle(new ExportFinalMediaReportPdfCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedBytes);
        await _pdfRenderer.Received(1).RenderAsync(Arg.Is<string>(html => html.Contains(report.Title)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureAndNeverCallsRenderer()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<byte[]> result = await _handler.Handle(new ExportFinalMediaReportPdfCommand(404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _pdfRenderer.DidNotReceive().RenderAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
