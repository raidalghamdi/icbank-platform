using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core mapping for <see cref="DownloadToken"/> -- new table added to close GAP 2
/// (FRONTEND-WIRING-NOTES.md §4: bearer-only download endpoints reached from a plain browser
/// navigation).
/// </summary>
public sealed class DownloadTokenConfig : IEntityTypeConfiguration<DownloadToken>
{
    private const int TokenHashMaxLength = 128;
    private const int ResourceTypeMaxLength = 40;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DownloadToken> builder)
    {
        builder.ToTable("download_tokens");
        builder.ConfigureAuditable();

        builder.Property(dt => dt.TokenHash).HasColumnName("token_hash").HasMaxLength(TokenHashMaxLength).IsRequired();
        builder.Property(dt => dt.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(ResourceTypeMaxLength).IsRequired();
        builder.Property(dt => dt.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(dt => dt.IssuedToUserId).HasColumnName("issued_to_user_id").IsRequired();
        builder.Property(dt => dt.ExpiresAt).HasColumnName("expires_at").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(dt => dt.UsedAt).HasColumnName("used_at").HasColumnType("datetime2(3)");

        builder.Ignore(dt => dt.IsRedeemable);

        // Unique + indexed: redemption looks the hash up on every request, and it must be unique
        // so a hash collision can never be mistaken for a different token (SHA-256 makes this a
        // theoretical concern only, but the JwtOptions/RefreshToken precedent enforces it anyway).
        builder.HasIndex(dt => dt.TokenHash).IsUnique().HasDatabaseName("ix_download_tokens_token_hash");

        // Supports "redeem for exactly this (type, id)" and lets an expiry-sweep job (future
        // work, not required by this task) find stale rows cheaply.
        builder.HasIndex(dt => new { dt.ResourceType, dt.ResourceId }).HasDatabaseName("ix_download_tokens_resource");
        builder.HasIndex(dt => dt.ExpiresAt).HasDatabaseName("ix_download_tokens_expires_at");
    }
}
