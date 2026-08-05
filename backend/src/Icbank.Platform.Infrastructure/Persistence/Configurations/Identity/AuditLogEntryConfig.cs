using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core mapping for <see cref="AuditLogEntry"/> — new dedicated privileged-action audit table
/// (DOTNET-CONVENTIONS.md §5.5; task requirement 5).
/// </summary>
public sealed class AuditLogEntryConfig : IEntityTypeConfiguration<AuditLogEntry>
{
    private const int ActionMaxLength = 150;
    private const int TargetTypeMaxLength = 100;
    private const int TargetIdMaxLength = 100;
    private const int CorrelationIdMaxLength = 100;
    private const int IpAddressMaxLength = 45;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.ConfigureAuditable();

        builder.Property(a => a.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.Property(a => a.Action).HasColumnName("action").HasMaxLength(ActionMaxLength).IsRequired();
        builder.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(TargetTypeMaxLength).IsRequired();
        builder.Property(a => a.TargetId).HasColumnName("target_id").HasMaxLength(TargetIdMaxLength).IsRequired();
        builder.Property(a => a.BeforeJson).HasColumnName("before_json").HasColumnType("nvarchar(max)");
        builder.Property(a => a.AfterJson).HasColumnName("after_json").HasColumnType("nvarchar(max)");
        builder.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(CorrelationIdMaxLength).IsRequired();
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(IpAddressMaxLength);

        builder.HasIndex(a => a.ActorUserId).HasDatabaseName("ix_audit_log_entries_actor_user_id");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_log_entries_created_at");
        builder.HasIndex(a => new { a.TargetType, a.TargetId }).HasDatabaseName("ix_audit_log_entries_target");

        // Restrict: an audit row must never disappear because the actor's account was later
        // deleted — the actor foreign key uses Restrict, matching the forensic-trail intent.
        builder.HasOne(a => a.ActorUser).WithMany()
            .HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
