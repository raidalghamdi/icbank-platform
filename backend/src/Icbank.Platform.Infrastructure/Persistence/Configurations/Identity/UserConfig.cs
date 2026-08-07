using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="User"/> (DATA-MODEL.md section 3.1 <c>users</c>).</summary>
public sealed class UserConfig : IEntityTypeConfiguration<User>
{
    private const int EmailMaxLength = 256;
    private const int NameMaxLength = 200;
    private const int TitleMaxLength = 200;
    private const int DepartmentMaxLength = 200;
    private const int PasswordHashMaxLength = 512;
    private const int AzureOidMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.ConfigureAuditable();

        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(EmailMaxLength).IsRequired();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(u => u.Title).HasColumnName("title").HasMaxLength(TitleMaxLength);
        builder.Property(u => u.Department).HasColumnName("department").HasMaxLength(DepartmentMaxLength);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(PasswordHashMaxLength);
        builder.Property(u => u.AzureOid).HasColumnName("azure_oid").HasMaxLength(AzureOidMaxLength);
        builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(u => u.IsLocked).HasColumnName("is_locked").IsRequired();
        builder.Property(u => u.FailedAttempts).HasColumnName("failed_attempts").IsRequired();
        builder.Property(u => u.LastLogin).HasColumnName("last_login").HasColumnType("datetime2(3)");
        builder.Property(u => u.PasswordChangedAt).HasColumnName("password_changed_at").HasColumnType("datetime2(3)");
        builder.Property(u => u.MustChangePassword).HasColumnName("must_change_password").IsRequired();

        // Unique per source schema (email/azure_oid both .unique() in rbac.ts).
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ux_users_email");
        builder.HasIndex(u => u.AzureOid).IsUnique().HasFilter("[azure_oid] IS NOT NULL").HasDatabaseName("ux_users_azure_oid");
    }
}
