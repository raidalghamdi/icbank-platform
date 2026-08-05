using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahWorkflowLog"/> (DATA-MODEL.md section 3.8 <c>shorfah_workflow_log</c>).</summary>
public sealed class ShorfahWorkflowLogConfig : IEntityTypeConfiguration<ShorfahWorkflowLog>
{
    private const int ActionMaxLength = 50;
    private const int StatusMaxLength = 30;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahWorkflowLog> builder)
    {
        builder.ToTable("shorfah_workflow_log");
        builder.ConfigureAuditable();

        builder.Property(l => l.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(l => l.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(l => l.Action).HasColumnName("action").HasMaxLength(ActionMaxLength).IsRequired();
        builder.Property(l => l.FromStatus).HasColumnName("from_status").HasMaxLength(StatusMaxLength);
        builder.Property(l => l.ToStatus).HasColumnName("to_status").HasMaxLength(StatusMaxLength);
        builder.Property(l => l.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");

        builder.HasIndex(l => l.SectionId).HasDatabaseName("ix_shorfah_workflow_log_section_id");
        builder.HasIndex(l => l.ActorUserId).HasDatabaseName("ix_shorfah_workflow_log_actor_user_id");

        // Cascade: the log is meaningless without its section (this table is itself the
        // append-only audit trail for the section, so it shares the section's lifecycle).
        builder.HasOne(l => l.Section).WithMany(s => s.WorkflowLogs)
            .HasForeignKey(l => l.SectionId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: preserves the audit trail's actor reference even if that user is later
        // deleted -- audit logs must never lose history to a cascading delete.
        builder.HasOne(l => l.ActorUser).WithMany()
            .HasForeignKey(l => l.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
