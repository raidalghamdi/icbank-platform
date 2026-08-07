using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="GetMediaReportByIdQueryHandler"/>.</summary>
public sealed class GetMediaReportByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly GetMediaReportByIdQueryHandler _handler;

    public GetMediaReportByIdQueryHandlerTests()
    {
        _handler = new GetMediaReportByIdQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_ExistingReport_ReturnsMappedDto()
    {
        _dbContext.MediaReports.Returns(new[] { new MediaReport { Id = 5, Title = "T", ContentMd = "c" } }.AsQueryable());

        Result<MediaReportDto> result = await _handler.Handle(new GetMediaReportByIdQuery(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("T");
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureWithArabicMessage()
    {
        _dbContext.MediaReports.Returns(Array.Empty<MediaReport>().AsQueryable());

        Result<MediaReportDto> result = await _handler.Handle(new GetMediaReportByIdQuery(99), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("التقرير غير موجود");
    }
}
