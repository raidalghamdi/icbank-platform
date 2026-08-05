using Azure;
using Azure.Storage.Blobs;
using FluentAssertions;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Infrastructure.Storage;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Storage;

/// <summary>
/// Verifies <see cref="AzureBlobObjectStorageReader"/> against substituted Azure SDK clients,
/// with particular attention to the not-found path: a missing blob must resolve to
/// <see langword="null"/> (the <see cref="IObjectStorageReader"/> contract), not an unhandled
/// <see cref="RequestFailedException"/>.
/// </summary>
public sealed class AzureBlobObjectStorageReaderTests
{
    private readonly BlobServiceClient _serviceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();

    public AzureBlobObjectStorageReaderTests()
    {
        _serviceClient.GetBlobContainerClient("weekend").Returns(_containerClient);
        _containerClient.GetBlobClient("abc123.png").Returns(_blobClient);
    }

    [Fact]
    public async Task OpenAsync_BlobNotFound_ReturnsNullRatherThanThrowing()
    {
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Response<Azure.Storage.Blobs.Models.BlobDownloadResult>>>(_ =>
                throw new RequestFailedException(status: 404, message: "The specified blob does not exist."));

        var reader = new AzureBlobObjectStorageReader(_serviceClient);

        StoredObject? result = await reader.OpenAsync("weekend/abc123.png", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task OpenAsync_OtherRequestFailure_PropagatesRatherThanBeingSwallowed()
    {
        // Why: only a 404 means "does not exist" (the one condition IObjectStorageReader's
        // contract treats as a null return). Any other status -- e.g. 403 permission drift, 500
        // transient outage -- is a genuine failure that must not be silently mapped to "no object
        // here", which would be indistinguishable from a legitimately missing file to the caller.
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Response<Azure.Storage.Blobs.Models.BlobDownloadResult>>>(_ =>
                throw new RequestFailedException(status: 403, message: "Forbidden"));

        var reader = new AzureBlobObjectStorageReader(_serviceClient);

        Func<Task> act = () => reader.OpenAsync("weekend/abc123.png", CancellationToken.None);

        await act.Should().ThrowAsync<RequestFailedException>();
    }
}
