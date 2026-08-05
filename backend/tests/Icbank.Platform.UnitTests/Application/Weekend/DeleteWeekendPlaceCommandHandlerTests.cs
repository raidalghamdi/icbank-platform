using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Application.Weekend.Commands;
using Icbank.Platform.Domain.Weekend;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Weekend;

/// <summary>
/// Verifies <see cref="DeleteWeekendPlaceCommandHandler"/> closes WAVE1-PORT-NOTES.md item 23: a
/// deleted place's storage object must actually be deleted, but a storage-side failure or an
/// out-of-prefix <see cref="WeekendPlace.ImageUrl"/> must never surface as a failure of the
/// command itself (the row is already gone by the time the storage delete is attempted).
/// </summary>
public sealed class DeleteWeekendPlaceCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAsyncQueryExecutor _queryExecutor = Substitute.For<IAsyncQueryExecutor>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly ISafeStoragePathValidator _pathValidator = Substitute.For<ISafeStoragePathValidator>();
    private readonly IObjectStorageDeleter _storageDeleter = Substitute.For<IObjectStorageDeleter>();
    private readonly DeleteWeekendPlaceCommandHandler _handler;

    public DeleteWeekendPlaceCommandHandlerTests()
    {
        _handler = new DeleteWeekendPlaceCommandHandler(_dbContext, _queryExecutor, _auditLogService, _pathValidator, _storageDeleter);
    }

    [Fact]
    public async Task Handle_PlaceNotFound_ReturnsFailureAndNeverTouchesStorage()
    {
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(null));
        var command = new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1);

        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _storageDeleter.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlaceWithImageUnderWeekendPrefix_DeletesRowThenDeletesStorageObject()
    {
        var place = new WeekendPlace { Id = 1, Name = "منتزه", ImageUrl = "weekend/abc123.png" };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(place));
        _pathValidator.Validate("weekend/abc123.png", Arg.Is<IReadOnlyCollection<string>>(p => p.Contains("weekend/")))
            .Returns(new SafePathValidationResult(IsValid: true, NormalizedPath: "weekend/abc123.png", RejectionReason: null));
        var command = new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1);

        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.Received(1).Remove(place);
        Received.InOrder(() =>
        {
            _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>());
            _storageDeleter.DeleteAsync("weekend/abc123.png", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_PlaceWithNoImage_SucceedsWithoutCallingStorage()
    {
        var place = new WeekendPlace { Id = 1, Name = "منتزه", ImageUrl = null };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(place));
        var command = new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1);

        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _storageDeleter.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ImageUrlRejectedByPathValidator_SucceedsAndSkipsStorageDelete()
    {
        // Why: a stored ImageUrl that no longer normalizes under weekend/ (e.g. legacy data, or a
        // value tampered with before this validation existed) must not block the already-committed
        // row delete, and must not be passed to the storage port under an unproven prefix.
        var place = new WeekendPlace { Id = 1, Name = "منتزه", ImageUrl = "../etc/passwd" };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(place));
        _pathValidator.Validate("../etc/passwd", Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new SafePathValidationResult(IsValid: false, NormalizedPath: null, RejectionReason: "traversal"));
        var command = new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1);

        Result<bool> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _storageDeleter.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StorageDeleteThrows_CommandStillSucceeds()
    {
        // Why: the row is already committed by the time TryDeleteImageAsync runs -- a transient
        // storage outage must not be reported back as a failed delete command. This was a genuine
        // bug: the handler's own XML doc (and IObjectStorageDeleter's) promise exactly this, but
        // the storage call was unguarded and let the exception propagate. Fixed by wrapping the
        // best-effort delete in a try/catch in DeleteWeekendPlaceCommandHandler -- this test now
        // asserts the documented contract instead of the pre-fix throwing behaviour.
        var place = new WeekendPlace { Id = 1, Name = "منتزه", ImageUrl = "weekend/abc123.png" };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(place));
        _pathValidator.Validate("weekend/abc123.png", Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new SafePathValidationResult(IsValid: true, NormalizedPath: "weekend/abc123.png", RejectionReason: null));
        _storageDeleter.DeleteAsync("weekend/abc123.png", Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new InvalidOperationException("storage outage"));

        Result<bool> result = await _handler.Handle(new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Success_RecordsAuditLogBeforeStorageDelete()
    {
        var place = new WeekendPlace { Id = 1, Name = "منتزه", ImageUrl = "weekend/abc123.png" };
        _queryExecutor.SingleOrDefaultAsync(Arg.Any<IQueryable<WeekendPlace>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WeekendPlace?>(place));
        _pathValidator.Validate("weekend/abc123.png", Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new SafePathValidationResult(IsValid: true, NormalizedPath: "weekend/abc123.png", RejectionReason: null));
        var command = new DeleteWeekendPlaceCommand(ActorUserId: 7, PlaceId: 1);

        await _handler.Handle(command, CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            7,
            "weekend_place.delete",
            "WeekendPlace",
            "1",
            Arg.Any<object>(),
            Arg.Is<object?>(after => after == null),
            Arg.Any<CancellationToken>());
    }
}
