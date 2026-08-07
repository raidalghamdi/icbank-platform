using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core mapping for <see cref="RefreshToken"/> — new table added to close SEC-05/AUTH-01/
/// AUTH-02 (DOTNET-CONVENTIONS.md §5.1: revocable, single-use, rotate on every refresh).
/// </summary>
public sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    private const int TokenHashMaxLength = 128;
    private const int IpAddressMaxLength = 45;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.ConfigureAuditable();

        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash").HasMaxLength(TokenHashMaxLength).IsRequired();
        builder.Property(rt => rt.ExpiresAt).HasColumnName("expires_at").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(rt => rt.RevokedAt).HasColumnName("revoked_at").HasColumnType("datetime2(3)");
        builder.Property(rt => rt.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(rt => rt.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(IpAddressMaxLength);

        builder.Ignore(rt => rt.IsActive);

        // Unique + indexed: the hash is looked up on every refresh call, and must be unique so a
        // hash collision can never be mistaken for a different user's live session.
        builder.HasIndex(rt => rt.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_token_hash");
        builder.HasIndex(rt => rt.UserId).HasDatabaseName("ix_refresh_tokens_user_id");

        builder.HasOne(rt => rt.User).WithMany()
            .HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
