using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="SystemSetting"/> (DATA-MODEL.md section 3.1 <c>system_settings</c>).</summary>
public sealed class SystemSettingConfig : IEntityTypeConfiguration<SystemSetting>
{
    private const int KeyMaxLength = 150;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.ConfigureAuditable();

        builder.Property(s => s.Key).HasColumnName("key").HasMaxLength(KeyMaxLength).IsRequired();
        builder.Property(s => s.Value).HasColumnName("value").HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(s => s.Key).IsUnique().HasDatabaseName("ux_system_settings_key");
    }
}
