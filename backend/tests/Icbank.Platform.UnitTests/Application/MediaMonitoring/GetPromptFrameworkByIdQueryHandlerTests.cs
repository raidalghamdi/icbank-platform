using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="GetPromptFrameworkByIdQueryHandler"/>.</summary>
public sealed class GetPromptFrameworkByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly GetPromptFrameworkByIdQueryHandler _handler;

    public GetPromptFrameworkByIdQueryHandlerTests()
    {
        _handler = new GetPromptFrameworkByIdQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_ExistingFramework_ReturnsMappedDto()
    {
        _dbContext.PromptFrameworks.Returns(new[] { new PromptFramework { Id = 9, NameAr = "اسم", PromptText = "نص" } }.AsQueryable());

        Result<PromptFrameworkDto> result = await _handler.Handle(new GetPromptFrameworkByIdQuery(9), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NameAr.Should().Be("اسم");
    }

    [Fact]
    public async Task Handle_MissingFramework_ReturnsFailure()
    {
        _dbContext.PromptFrameworks.Returns(Array.Empty<PromptFramework>().AsQueryable());

        Result<PromptFrameworkDto> result = await _handler.Handle(new GetPromptFrameworkByIdQuery(404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("القالب غير موجود");
    }
}
