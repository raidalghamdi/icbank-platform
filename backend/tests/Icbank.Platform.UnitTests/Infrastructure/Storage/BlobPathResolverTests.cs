using Azure.Storage.Blobs;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Storage;

namespace Icbank.Platform.UnitTests.Infrastructure.Storage;

/// <summary>
/// Verifies <see cref="BlobPathResolver"/>'s folder-prefix-to-container mapping: the shared logic
/// every Azure Blob adapter (reader, writer, upload-URL issuer, deleter) relies on to agree on
/// exactly the same container for a given normalized relative object path.
/// </summary>
public sealed class BlobPathResolverTests
{
    // Why: a real credential is never contacted by this test -- BlobServiceClient's
    // GetBlobContainerClient(name) is a pure, local URI-composition call that never makes a
    // network request, so a client constructed with any well-formed URI is sufficient here.
    private static readonly BlobServiceClient ServiceClient = new(new Uri("https://unit-test-account.blob.core.windows.net"));

    [Theory]
    [InlineData("weekend/abc123.png", "weekend", "abc123.png")]
    [InlineData("designs/generated/abc123.png", "designs", "generated/abc123.png")]
    [InlineData("shorfah/issues/2026-01/cover.pdf", "shorfah", "issues/2026-01/cover.pdf")]
    [InlineData("media-reports/final/report.pdf", "media-reports", "final/report.pdf")]
    [InlineData("ai-year/2026/activation.json", "ai-year", "2026/activation.json")]
    public void Resolve_FolderPrefixPath_MapsToExpectedContainerAndBlobName(string path, string expectedContainer, string expectedBlobName)
    {
        (BlobContainerClient container, var blobName) = BlobPathResolver.Resolve(ServiceClient, path);

        container.Name.Should().Be(expectedContainer);
        blobName.Should().Be(expectedBlobName);
    }

    [Fact]
    public void Resolve_LeadingSlash_IsTrimmedBeforeSplitting()
    {
        (BlobContainerClient container, var blobName) = BlobPathResolver.Resolve(ServiceClient, "/weekend/abc123.png");

        container.Name.Should().Be("weekend");
        blobName.Should().Be("abc123.png");
    }

    [Theory]
    [InlineData("no-folder-segment")]
    [InlineData("")]
    public void Resolve_PathWithNoFolderSegment_ThrowsArgumentException(string path)
    {
        Action act = () => BlobPathResolver.Resolve(ServiceClient, path);

        act.Should().Throw<ArgumentException>().WithParameterName("normalizedRelativePath");
    }

    [Fact]
    public void Resolve_PathEndingInSlashWithNothingAfter_ThrowsArgumentException()
    {
        // Why: "weekend/" has a separator but no blob name after it -- a container the resolver
        // could find is not, by itself, a valid object path.
        Action act = () => BlobPathResolver.Resolve(ServiceClient, "weekend/");

        act.Should().Throw<ArgumentException>().WithParameterName("normalizedRelativePath");
    }

    [Fact]
    public void Resolve_PathStartingWithSlashOnly_ThrowsArgumentException()
    {
        // Why: after TrimStart('/'), "/" becomes "" -- IndexOf('/') is -1, which must be rejected
        // by the <= 0 branch rather than throwing an unrelated substring exception.
        Action act = () => BlobPathResolver.Resolve(ServiceClient, "/");

        act.Should().Throw<ArgumentException>().WithParameterName("normalizedRelativePath");
    }
}
