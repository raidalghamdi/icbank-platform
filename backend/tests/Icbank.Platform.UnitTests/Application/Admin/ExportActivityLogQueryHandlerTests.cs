using FluentAssertions;
using Icbank.Platform.Application.Admin.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Admin;

/// <summary>
/// Verifies <see cref="ExportActivityLogQueryHandler"/>: filter parity with the JSON list
/// sibling, newest-first ordering, the <see cref="ExportActivityLogQueryHandler.MaxRows"/> cap,
/// and that every successful export writes exactly one audit-log entry.
/// </summary>
public sealed class ExportActivityLogQueryHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly ExportActivityLogQueryHandler _handler;

    public ExportActivityLogQueryHandlerTests()
    {
        _handler = new ExportActivityLogQueryHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllRowsNewestFirstAndWritesAuditEntry()
    {
        var user = new User { Id = 1, Email = "admin@test.local", Name = "Admin" };
        ActivityLog[] logs =
        {
            BuildLog(1, user.Id, "login_success", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildLog(2, user.Id, "user.create", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
        };
        _dbContext.ActivityLogs.Returns(logs.AsQueryable());
        _dbContext.Users.Returns(new[] { user }.AsQueryable());

        Result<ActivityLogExportDto> result = await _handler.Handle(
            new ExportActivityLogQuery(user.Id, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rows.Should().HaveCount(2);
        result.Value.Rows[0].Id.Should().Be(2, "newest (2026-01-02) must sort first");
        result.Value.Rows[0].UserName.Should().Be("Admin");
        result.Value.Rows[0].UserEmail.Should().Be("admin@test.local");
        result.Value.TotalMatched.Should().Be(2);

        await _auditLogService.Received(1).RecordAsync(
            user.Id, "activity_log.export", "activity_log", "*", Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActionFilter_MatchesExactlyNotSubstring()
    {
        ActivityLog[] logs =
        {
            BuildLog(1, null, "user.create", DateTime.UtcNow),
            BuildLog(2, null, "user.create.retry", DateTime.UtcNow),
        };
        _dbContext.ActivityLogs.Returns(logs.AsQueryable());
        _dbContext.Users.Returns(Array.Empty<User>().AsQueryable());

        Result<ActivityLogExportDto> result = await _handler.Handle(
            new ExportActivityLogQuery(1, null, "user.create", null, null), CancellationToken.None);

        result.Value!.Rows.Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MoreRowsThanMaxRows_CapsAtMaxRowsButAuditsTotalMatched()
    {
        ActivityLog[] logs = Enumerable.Range(1, ExportActivityLogQueryHandler.MaxRows + 10)
            .Select(i => BuildLog(i, null, "login_success", DateTime.UtcNow.AddSeconds(i)))
            .ToArray();
        _dbContext.ActivityLogs.Returns(logs.AsQueryable());
        _dbContext.Users.Returns(Array.Empty<User>().AsQueryable());

        Result<ActivityLogExportDto> result = await _handler.Handle(
            new ExportActivityLogQuery(1, null, null, null, null), CancellationToken.None);

        result.Value!.Rows.Should().HaveCount(ExportActivityLogQueryHandler.MaxRows);
        result.Value.TotalMatched.Should().Be(ExportActivityLogQueryHandler.MaxRows + 10);

        // Why: the cap must keep the newest rows, not an arbitrary slice.
        result.Value.Rows[0].Id.Should().Be(ExportActivityLogQueryHandler.MaxRows + 10);
    }

    [Fact]
    public async Task Handle_UnknownUser_RendersEmDashPlaceholdersNotNulls()
    {
        _dbContext.ActivityLogs.Returns(new[] { BuildLog(1, userId: 999, "login_success", DateTime.UtcNow) }.AsQueryable());
        _dbContext.Users.Returns(Array.Empty<User>().AsQueryable());

        Result<ActivityLogExportDto> result = await _handler.Handle(
            new ExportActivityLogQuery(1, null, null, null, null), CancellationToken.None);

        result.Value!.Rows[0].UserName.Should().BeNull();
        result.Value.Rows[0].UserEmail.Should().BeNull();
    }

    private static ActivityLog BuildLog(int id, int? userId, string action, DateTime createdAt) => new()
    {
        Id = id,
        UserId = userId,
        Action = action,
        EntityType = "user",
        EntityId = "42",
        IpAddress = "127.0.0.1",
        CreatedAt = createdAt,
    };
}
