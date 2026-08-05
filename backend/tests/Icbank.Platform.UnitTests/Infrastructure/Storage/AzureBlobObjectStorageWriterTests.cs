using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Storage;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Storage;

/// <summary>
/// Verifies <see cref="AzureBlobObjectStorageWriter"/> resolves the container from the requested
/// folder prefix, uploads with the caller-supplied content type, and returns a relative path
/// under that same prefix -- all against substituted Azure SDK clients, no live storage account.
/// </summary>
public sealed class AzureBlobObjectStorageWriterTests
{
    private readonly BlobServiceClient _serviceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();

    public AzureBlobObjectStorageWriterTests()
    {
        _serviceClient.GetBlobContainerClient("weekend").Returns(_containerClient);
        _containerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
    }

    [Fact]
    public async Task SaveAsync_ReturnsRelativePathUnderRequestedFolderPrefix()
    {
        var writer = new AzureBlobObjectStorageWriter(_serviceClient);

        var relativePath = await writer.SaveAsync("weekend", new byte[] { 1, 2, 3 }, "image/png", CancellationToken.None);

        relativePath.Should().StartWith("weekend/");
        relativePath.Should().EndWith(".png");
    }

    [Fact]
    public async Task SaveAsync_UploadsWithCallerSuppliedContentType()
    {
        var writer = new AzureBlobObjectStorageWriter(_serviceClient);

        await writer.SaveAsync("weekend", new byte[] { 1, 2, 3 }, "image/png", CancellationToken.None);

        await _blobClient.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<BlobUploadOptions>(o => o.HttpHeaders!.ContentType == "image/png"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_GeneratesAUniqueObjectNamePerCall()
    {
        var writer = new AzureBlobObjectStorageWriter(_serviceClient);

        var first = await writer.SaveAsync("weekend", new byte[] { 1 }, "image/png", CancellationToken.None);
        var second = await writer.SaveAsync("weekend", new byte[] { 1 }, "image/png", CancellationToken.None);

        first.Should().NotBe(second);
    }
}
