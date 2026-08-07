using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="ActivityLog"/> (DATA-MODEL.md section 3.1 <c>activity_logs</c>).</summary>
public sealed class ActivityLogConfig : IEntityTypeConfiguration<ActivityLog>
{
    private const int ActionMaxLength = 100;
    private const int EntityTypeMaxLength = 100;
    private const int EntityIdMaxLength = 100;
    private const int IpAddressMaxLength = 45;
    private const int UserAgentMaxLength = 512;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");
        builder.ConfigureAuditable();

        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(ActionMaxLength).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(EntityTypeMaxLength);
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(EntityIdMaxLength);
        builder.Property(a => a.DetailsJson).HasColumnName("details_json").HasColumnType("nvarchar(max)");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(IpAddressMaxLength);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(UserAgentMaxLength);

        // Deviation: DATA-MODEL.md section 3.1 (DATA-07) flags missing indexes on user_id and
        // created_at despite both being queried by the admin activity feed. Added here.
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_activity_logs_user_id");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_activity_logs_created_at");

        // SetNull: matches source .references(..., { onDelete: "set null" }) -- a log entry must
        // survive the deletion of the acting user for audit-trail completeness.
        builder.HasOne(a => a.User).WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
