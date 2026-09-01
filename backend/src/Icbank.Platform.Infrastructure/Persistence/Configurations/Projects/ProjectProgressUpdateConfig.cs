using Icbank.Platform.Domain.Projects;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Projects;

/// <summary>EF Core mapping for <see cref="ProjectProgressUpdate"/>.</summary>
public sealed class ProjectProgressUpdateConfig : IEntityTypeConfiguration<ProjectProgressUpdate>
{
    private const int NoteMaxLength = 600;
    private const int ReportedByMaxLength = 150;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectProgressUpdate> builder)
    {
        builder.ToTable("project_progress_updates");
        builder.ConfigureAuditable();

        builder.Property(u => u.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(u => u.ProgressPercent).HasColumnName("progress_percent").IsRequired();
        builder.Property(u => u.Note).HasColumnName("note").HasMaxLength(NoteMaxLength).IsRequired();
        builder.Property(u => u.ReportedBy).HasColumnName("reported_by").HasMaxLength(ReportedByMaxLength).IsRequired();
        builder.Property(u => u.ReportedAt).HasColumnName("reported_at").HasColumnType("datetime2(3)").IsRequired();

        builder.HasOne(u => u.Project)
            .WithMany(p => p.ProgressUpdates)
            .HasForeignKey(u => u.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Why: the history is only ever read newest-first for one project, so the index carries the
        // sort direction instead of leaving the server to sort every read.
        builder.HasIndex(u => new { u.ProjectId, u.ReportedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_project_progress_updates_project_reported_at");
    }
}
