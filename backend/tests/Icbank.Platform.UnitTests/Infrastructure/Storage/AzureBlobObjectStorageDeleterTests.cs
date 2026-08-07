using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Storage;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Storage;

/// <summary>
/// Verifies <see cref="AzureBlobObjectStorageDeleter"/> against substituted Azure SDK clients
/// (<see cref="BlobServiceClient"/>, <see cref="BlobContainerClient"/>, <see cref="BlobClient"/>
/// all expose the members this adapter calls as <c>virtual</c>, which is exactly what makes them
/// mockable without a live Azure Storage account -- see WAVE1-PORT-NOTES.md and the Azure SDK's
/// own design-for-testability guidance).
/// </summary>
public sealed class AzureBlobObjectStorageDeleterTests
{
    private readonly BlobServiceClient _serviceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();

    public AzureBlobObjectStorageDeleterTests()
    {
        _serviceClient.GetBlobContainerClient("weekend").Returns(_containerClient);
        _containerClient.GetBlobClient("abc123.png").Returns(_blobClient);
    }

    [Fact]
    public async Task DeleteAsync_ObjectExists_ReturnsTrueAndDeletesIncludingSnapshots()
    {
        var response = Response.FromValue(true, Substitute.For<Response>());
        _blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var deleter = new AzureBlobObjectStorageDeleter(_serviceClient);

        var result = await deleter.DeleteAsync("weekend/abc123.png", CancellationToken.None);

        result.Should().BeTrue();
        await _blobClient.Received(1).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ObjectAlreadyGone_ReturnsFalseRatherThanThrowing()
    {
        // Why: IObjectStorageDeleter's contract (Application.Storage.IObjectStorageDeleter) is
        // explicit -- deleting something already gone is not an error, because the caller's goal
        // ("this object must not exist afterward") is already satisfied. DeleteIfExistsAsync
        // itself returns false rather than throwing in this case, which this adapter must surface
        // as-is.
        var response = Response.FromValue(false, Substitute.For<Response>());
        _blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var deleter = new AzureBlobObjectStorageDeleter(_serviceClient);

        var result = await deleter.DeleteAsync("weekend/abc123.png", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ResolvesContainerAndBlobNameFromNormalizedPath()
    {
        var response = Response.FromValue(true, Substitute.For<Response>());
        _blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        var deleter = new AzureBlobObjectStorageDeleter(_serviceClient);

        await deleter.DeleteAsync("weekend/abc123.png", CancellationToken.None);

        _serviceClient.Received(1).GetBlobContainerClient("weekend");
        _containerClient.Received(1).GetBlobClient("abc123.png");
    }
}
