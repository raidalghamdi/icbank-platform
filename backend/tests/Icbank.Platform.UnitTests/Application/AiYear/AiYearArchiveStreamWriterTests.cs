using System.IO.Compression;
using FluentAssertions;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Storage;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Application.AiYear;

/// <summary>
/// Tests the real ZIP-streaming archive writer that replaced the Wave 2 manifest-only
/// placeholder for <c>GET /ai-year/activations/{id}/zip</c>.
/// </summary>
public sealed class AiYearArchiveStreamWriterTests
{
    private readonly ISafeStoragePathValidator _pathValidator = Substitute.For<ISafeStoragePathValidator>();
    private readonly IObjectStorageReader _storageReader = Substitute.For<IObjectStorageReader>();
    private readonly AiYearArchiveStreamWriter _writer;

    public AiYearArchiveStreamWriterTests()
    {
        _writer = new AiYearArchiveStreamWriter(_pathValidator, _storageReader);
    }

    [Fact]
    public async Task WriteAsync_TwoValidEntries_ProducesZipWithBothEntries()
    {
        ArrangeValidEntry("photo1.jpg", "ai-year/2026/photo1.jpg", "photo one bytes"u8.ToArray());
        ArrangeValidEntry("photo2.jpg", "ai-year/2026/photo2.jpg", "photo two bytes"u8.ToArray());
        AiYearArchiveEntryDto[] entries = new[]
        {
            new AiYearArchiveEntryDto("photo1.jpg", "ai-year/2026/photo1.jpg"),
            new AiYearArchiveEntryDto("photo2.jpg", "ai-year/2026/photo2.jpg"),
        };

        using var destination = new MemoryStream();
        var writtenCount = await _writer.WriteAsync(entries, destination, CancellationToken.None);

        writtenCount.Should().Be(2);
        destination.Position = 0;
        using var archive = new ZipArchive(destination, ZipArchiveMode.Read);
        archive.Entries.Select(e => e.Name).Should().BeEquivalentTo("photo1.jpg", "photo2.jpg");
    }

    [Fact]
    public async Task WriteAsync_MissingStorageObject_SkipsEntryWithoutThrowing()
    {
        ArrangeValidEntry("found.jpg", "ai-year/2026/found.jpg", "bytes"u8.ToArray());
        _pathValidator.Validate("ai-year/2026/missing.jpg", Arg.Any<IReadOnlyCollection<string>>())
            .Returns(SafePathValidationResult.Valid("ai-year/2026/missing.jpg"));
        _storageReader.OpenAsync("ai-year/2026/missing.jpg", Arg.Any<CancellationToken>()).Returns((StoredObject?)null);

        AiYearArchiveEntryDto[] entries = new[]
        {
            new AiYearArchiveEntryDto("found.jpg", "ai-year/2026/found.jpg"),
            new AiYearArchiveEntryDto("missing.jpg", "ai-year/2026/missing.jpg"),
        };

        using var destination = new MemoryStream();
        var writtenCount = await _writer.WriteAsync(entries, destination, CancellationToken.None);

        writtenCount.Should().Be(1, "a missing backing object is skipped, matching the Node source's per-file ObjectNotFoundError catch-and-continue");
        destination.Position = 0;
        using var archive = new ZipArchive(destination, ZipArchiveMode.Read);
        archive.Entries.Should().ContainSingle().Which.Name.Should().Be("found.jpg");
    }

    [Fact]
    public async Task WriteAsync_PathFailsValidation_SkipsEntryWithoutThrowing()
    {
        _pathValidator.Validate("../etc/passwd", Arg.Any<IReadOnlyCollection<string>>())
            .Returns(SafePathValidationResult.Invalid("traversal_segment"));

        AiYearArchiveEntryDto[] entries = new[] { new AiYearArchiveEntryDto("evil.jpg", "../etc/passwd") };

        using var destination = new MemoryStream();
        var writtenCount = await _writer.WriteAsync(entries, destination, CancellationToken.None);

        writtenCount.Should().Be(0);
        await _storageReader.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WriteAsync_EmptyEntryList_ProducesEmptyButValidZip()
    {
        using var destination = new MemoryStream();
        var writtenCount = await _writer.WriteAsync(Array.Empty<AiYearArchiveEntryDto>(), destination, CancellationToken.None);

        writtenCount.Should().Be(0);
        destination.Position = 0;
        using var archive = new ZipArchive(destination, ZipArchiveMode.Read);
        archive.Entries.Should().BeEmpty();
    }

    private void ArrangeValidEntry(string entryName, string objectPath, byte[] content)
    {
        _pathValidator.Validate(objectPath, Arg.Any<IReadOnlyCollection<string>>())
            .Returns(SafePathValidationResult.Valid(objectPath));
        _storageReader.OpenAsync(objectPath, Arg.Any<CancellationToken>())
            .Returns(new StoredObject(content, "image/jpeg"));
    }
}
