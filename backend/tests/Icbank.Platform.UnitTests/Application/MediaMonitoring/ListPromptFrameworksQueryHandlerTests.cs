using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="ListPromptFrameworksQueryHandler"/> only returns active frameworks and honours category/kind filters + pagination (R-BE-033).</summary>
public sealed class ListPromptFrameworksQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly ListPromptFrameworksQueryHandler _handler;

    public ListPromptFrameworksQueryHandlerTests()
    {
        _handler = new ListPromptFrameworksQueryHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_RetiredFrameworksExist_ExcludesThemFromResults()
    {
        _dbContext.PromptFrameworks.Returns(new[]
        {
            MakeFramework(1, PromptFrameworkStatus.Active),
            MakeFramework(2, PromptFrameworkStatus.Retired),
        }.AsQueryable());
        var query = new ListPromptFrameworksQuery(new PagedQuery(), null, null);

        Result<PagedResult<PromptFrameworkDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(f => f.Id == 1);
    }

    [Fact]
    public async Task Handle_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        PromptFramework mediaReport = MakeFramework(1, PromptFrameworkStatus.Active);
        mediaReport.Category = PromptFrameworkCategory.MediaReport;
        PromptFramework analysis = MakeFramework(2, PromptFrameworkStatus.Active);
        analysis.Category = PromptFrameworkCategory.Analysis;
        _dbContext.PromptFrameworks.Returns(new[] { mediaReport, analysis }.AsQueryable());
        var query = new ListPromptFrameworksQuery(new PagedQuery(), "MediaReport", null);

        Result<PagedResult<PromptFrameworkDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(f => f.Id == 1);
    }

    [Fact]
    public async Task Handle_KindFilter_ReturnsOnlyMatchingKind()
    {
        PromptFramework template = MakeFramework(1, PromptFrameworkStatus.Active);
        template.Kind = PromptFrameworkKind.Template;
        PromptFramework framework = MakeFramework(2, PromptFrameworkStatus.Active);
        framework.Kind = PromptFrameworkKind.Framework;
        _dbContext.PromptFrameworks.Returns(new[] { template, framework }.AsQueryable());
        var query = new ListPromptFrameworksQuery(new PagedQuery(), null, "Template");

        Result<PagedResult<PromptFrameworkDto>> result = await _handler.Handle(query, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle(f => f.Id == 1);
    }

    private static PromptFramework MakeFramework(int id, PromptFrameworkStatus status) => new()
    {
        Id = id,
        NameAr = $"إطار {id}",
        PromptText = "نص",
        Status = status,
        CreatedAt = DateTime.UtcNow.AddMinutes(-id),
    };
}
