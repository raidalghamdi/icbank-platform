using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.Storage;

/// <summary>
/// Verifies <see cref="AzureBlobObjectUploadUrlIssuer"/> produces a short-lived, single-blob,
/// write-only user-delegation SAS URL without ever needing a storage account key or a live Azure
/// Storage account (BUSINESS-RULES.md §12.3).
/// </summary>
public sealed class AzureBlobObjectUploadUrlIssuerTests
{
    private const string AccountName = "icbankdevstorage";
    private const int LifetimeMinutes = 15;

    private readonly BlobServiceClient _serviceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly AzureBlobObjectUploadUrlIssuer _issuer;

    public AzureBlobObjectUploadUrlIssuerTests()
    {
        _containerClient.Name.Returns("weekend");
        _serviceClient.GetBlobContainerClient("weekend").Returns(_containerClient);
        _containerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
        _blobClient.Uri.Returns(new Uri($"https://{AccountName}.blob.core.windows.net/weekend/placeholder.png"));
        _blobClient.AccountName.Returns(AccountName);
        _blobClient.BlobContainerName.Returns("weekend");
        _blobClient.Name.Returns("placeholder.png");

        UserDelegationKey delegationKey = BlobsModelFactory.UserDelegationKey(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(LifetimeMinutes),
            "b",
            "2024-11-04",
            "dGVzdC1kZWxlZ2F0aW9uLWtleS12YWx1ZQ==");

        _serviceClient
            .GetUserDelegationKeyAsync(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Response.FromValue(delegationKey, Substitute.For<Response>())));

        IOptions<AzureBlobStorageOptions> options = Options.Create(new AzureBlobStorageOptions
        {
            ServiceUri = $"https://{AccountName}.blob.core.windows.net",
            AccountName = AccountName,
            UploadUrlLifetimeMinutes = LifetimeMinutes,
        });

        _issuer = new AzureBlobObjectUploadUrlIssuer(_serviceClient, options);
    }

    [Fact]
    public async Task IssueAsync_ReturnsUrlCarryingSasQueryParameters()
    {
        PresignedUpload result = await _issuer.IssueAsync("weekend", "photo.png", "image/png", CancellationToken.None);

        var uri = new Uri(result.UploadUrl);
        uri.Query.Should().Contain("sv=", "a SAS URL must carry the signed-version query parameter");
        uri.Query.Should().Contain("sig=", "a SAS URL must carry the signature query parameter");
    }

    [Fact]
    public async Task IssueAsync_SasExpiryIsWithinConfiguredLifetime()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        PresignedUpload result = await _issuer.IssueAsync("weekend", "photo.png", "image/png", CancellationToken.None);

        var uri = new Uri(result.UploadUrl);
        System.Collections.Specialized.NameValueCollection query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var expiry = DateTimeOffset.Parse(query["se"]!, System.Globalization.CultureInfo.InvariantCulture);

        // Why: the URL must be genuinely short-lived -- expiry must land at or before
        // "now + configured lifetime", not sometime arbitrarily further out.
        expiry.Should().BeOnOrBefore(before.AddMinutes(LifetimeMinutes).AddSeconds(5));
        expiry.Should().BeAfter(before);
    }

    [Fact]
    public async Task IssueAsync_ReturnsObjectPathUnderRequestedFolderPrefix()
    {
        PresignedUpload result = await _issuer.IssueAsync("weekend", "photo.png", "image/png", CancellationToken.None);

        result.ObjectPath.Should().StartWith("weekend/");
        result.ObjectPath.Should().EndWith(".png");
    }

    [Fact]
    public async Task IssueAsync_RequestsWriteAndCreatePermissionsOnly()
    {
        // Why: BUSINESS-RULES.md §12.3's presigned-upload flow only ever needs the client to PUT
        // once -- a broader permission set (read/delete/list) would let a leaked or replayed URL
        // do more than upload the one blob it was issued for.
        PresignedUpload result = await _issuer.IssueAsync("weekend", "photo.png", "image/png", CancellationToken.None);

        var uri = new Uri(result.UploadUrl);
        System.Collections.Specialized.NameValueCollection query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var permissions = query["sp"];

        permissions.Should().NotBeNullOrEmpty();
        permissions.Should().Contain("w");
        permissions.Should().NotContain("r");
        permissions.Should().NotContain("d");
    }

    [Fact]
    public async Task IssueAsync_UsesManagedIdentityDerivedDelegationKeyNotAnAccountKey()
    {
        await _issuer.IssueAsync("weekend", "photo.png", "image/png", CancellationToken.None);

        // Why: the only Azure Storage credential path this adapter is allowed to use is a user
        // delegation key obtained via the API's own managed identity -- asserting this call
        // happened is what proves no storage account key is ever generated or read.
        await _serviceClient.Received(1).GetUserDelegationKeyAsync(
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
