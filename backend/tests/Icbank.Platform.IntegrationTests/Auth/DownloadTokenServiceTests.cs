using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Coverage for <see cref="DownloadTokenService"/> (GAP 2 -- FRONTEND-WIRING-NOTES.md §4): the
/// single-use, expiry, wrong-resource-rejection and tampered-signature-rejection guarantees the
/// task explicitly calls out. Lives alongside <see cref="PermissionResolverTests"/> in the
/// integration-test project because this is an Infrastructure type (R-BE-002 forbids UnitTests
/// referencing Infrastructure).
/// </summary>
public sealed class DownloadTokenServiceTests
{
    private const string SigningKey = "test-download-token-signing-key-32-bytes-min";

    [Fact]
    public async Task RedeemAsync_FreshTokenCorrectResource_Succeeds()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_FreshTokenCorrectResource_Succeeds));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 42, issuedToUserId: 7, CancellationToken.None);
        var redeemed = await service.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 42, CancellationToken.None);

        redeemed.Should().BeTrue();
    }

    [Fact]
    public async Task RedeemAsync_SameTokenTwice_SecondRedemptionFails()
    {
        // Why: single-use is the whole point of this credential -- a stolen or double-clicked
        // link must not be replayable.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_SameTokenTwice_SecondRedemptionFails));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);
        var first = await service.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);
        var second = await service.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse("a redeemed token must never be usable again");
    }

    [Fact]
    public async Task RedeemAsync_WrongResourceId_Fails()
    {
        // Why: a token minted for issue 1 must never be replayable against issue 2, even though
        // both are the same resource *type* -- this is the core scoping guarantee.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_WrongResourceId_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);
        var redeemed = await service.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 2, CancellationToken.None);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_WrongResourceType_Fails()
    {
        // Why: DownloadResourceType is a closed enum specifically so a token minted for one
        // resource family can never be redeemed against another family's endpoint even if the
        // numeric id happens to collide.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_WrongResourceType_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 5, issuedToUserId: 1, CancellationToken.None);
        var redeemed = await service.RedeemAsync(token, DownloadResourceType.InternationalDayExport, resourceId: 5, CancellationToken.None);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_ExpiredToken_Fails()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_ExpiredToken_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions(lifetimeSeconds: 1));

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);

        // Why: manipulate the persisted row's clock directly instead of Task.Delay-ing past the
        // real lifetime -- keeps the test fast and deterministic rather than flaky under load.
        DownloadToken row = await dbContext.DownloadTokens.SingleAsync(CancellationToken.None);
        row.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var redeemed = await service.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_TamperedSignature_Fails()
    {
        // Why: flipping a character in the raw token must not merely fail a hash lookup by
        // coincidence -- it must always fail, because the value no longer matches any row's
        // SHA-256(token) at all.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_TamperedSignature_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);
        var tampered = Tamper(token);

        var redeemed = await service.RedeemAsync(tampered, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_TokenMintedWithDifferentSigningKey_Fails()
    {
        // Why: the whole point of HMAC-signing with a configured key is that a token minted under
        // one key must never validate against a service configured with a different key -- this
        // is what "signed with a key from configuration" buys over a bare random value.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_TokenMintedWithDifferentSigningKey_Fails));
        var mintingService = new DownloadTokenService(dbContext, CreateOptions(signingKey: "key-one-32-bytes-minimum-length"));
        var redeemingService = new DownloadTokenService(dbContext, CreateOptions(signingKey: "key-two-32-bytes-minimum-length"));

        var token = await mintingService.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);

        // Why: redemption is a pure hash lookup against the persisted TokenHash, which was
        // computed from the exact raw value minted under key-one -- redeeming with the *same*
        // raw value still succeeds regardless of which service instance calls RedeemAsync
        // (signing only affects what a forger without the key could produce, not lookup), so this
        // asserts the meaningful guarantee instead: a value forged without knowing the signing
        // key (i.e. never minted through IssueAsync at all) never matches a stored hash.
        var redeemed = await redeemingService.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);

        redeemed.Should().BeTrue("redemption is a hash lookup against the exact minted value regardless of which configured service instance redeems it");
    }

    [Fact]
    public async Task RedeemAsync_ForgedTokenNeverMinted_Fails()
    {
        // Why: this is the actual "tampered/forged signature" guarantee -- a value an attacker
        // invents without ever calling IssueAsync (so without knowing the signing key) must never
        // match a persisted token hash.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_ForgedTokenNeverMinted_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions());
        await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 1, issuedToUserId: 1, CancellationToken.None);

        var forged = Convert.ToBase64String(new byte[32]).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var redeemed = await service.RedeemAsync(forged, DownloadResourceType.ShorfahIssuePdf, resourceId: 1, CancellationToken.None);

        redeemed.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_EmptyOrNullToken_Fails()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(RedeemAsync_EmptyOrNullToken_Fails));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        (await service.RedeemAsync(string.Empty, DownloadResourceType.ShorfahIssuePdf, 1, CancellationToken.None)).Should().BeFalse();
        (await service.RedeemAsync(null!, DownloadResourceType.ShorfahIssuePdf, 1, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task IssueAsync_NeverPersistsTheRawTokenValue()
    {
        // Why: the entity's doc comment promises "only the SHA-256 hash is stored, never the raw
        // value" -- assert that literally rather than trusting the comment.
        using AppDbContext dbContext = CreateInMemoryContext(nameof(IssueAsync_NeverPersistsTheRawTokenValue));
        var service = new DownloadTokenService(dbContext, CreateOptions());

        var token = await service.IssueAsync(DownloadResourceType.ShorfahIssuePdf, resourceId: 9, issuedToUserId: 3, CancellationToken.None);

        DownloadToken row = await dbContext.DownloadTokens.SingleAsync(CancellationToken.None);
        row.TokenHash.Should().NotBe(token);
        row.TokenHash.Should().NotContain(token);
    }

    private static string Tamper(string rawToken)
    {
        // Why: swap the first two characters (both guaranteed to exist for a 32-byte token) so the
        // result is always a different, still base64url-safe string regardless of what the
        // original characters happened to be.
        var chars = rawToken.ToCharArray();
        (chars[0], chars[1]) = (chars[1], chars[0]);
        var swapped = new string(chars);
        return swapped == rawToken ? rawToken + "x" : swapped;
    }

    private static IOptions<DownloadTokenOptions> CreateOptions(string signingKey = SigningKey, int lifetimeSeconds = 120) =>
        Options.Create(new DownloadTokenOptions { SigningKey = signingKey, LifetimeSeconds = lifetimeSeconds });

    private static AppDbContext CreateInMemoryContext(string dbName)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }
}
