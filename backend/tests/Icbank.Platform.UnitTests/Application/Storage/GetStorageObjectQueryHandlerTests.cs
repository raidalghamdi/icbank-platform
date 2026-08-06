using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Application.Storage.Queries;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Storage;

/// <summary>
/// Proves the authenticated storage proxy accepts the established weekend-place image taxonomy
/// without widening its traversal-safe validation to arbitrary paths.
/// </summary>
public sealed class GetStorageObjectQueryHandlerTests
{
    private readonly ISafeStoragePathValidator _pathValidator = Substitute.For<ISafeStoragePathValidator>();
    private readonly IObjectStorageReader _storageReader = Substitute.For<IObjectStorageReader>();

    [Fact]
    public async Task Handle_WeekendPlaceImageUnderValidatedPrefix_ReturnsStoredObject()
    {
        var stored = new StoredObject(new byte[] { 1, 2, 3 }, "image/png");
        _pathValidator.Validate(
                "weekend/place.png",
                Arg.Is<IReadOnlyCollection<string>>(prefixes => prefixes.Contains("weekend/")))
            .Returns(new SafePathValidationResult(true, "weekend/place.png", null));
        _storageReader.OpenAsync("weekend/place.png", Arg.Any<CancellationToken>())
            .Returns(stored);
        var handler = new GetStorageObjectQueryHandler(_pathValidator, _storageReader);

        Result<StoredObject> result = await handler.Handle(new GetStorageObjectQuery("weekend/place.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(stored);
    }
}
