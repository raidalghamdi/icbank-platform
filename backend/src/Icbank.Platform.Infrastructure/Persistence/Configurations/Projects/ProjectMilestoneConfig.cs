using Icbank.Platform.Domain.Projects;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Projects;

/// <summary>EF Core mapping for <see cref="ProjectMilestone"/>.</summary>
public sealed class ProjectMilestoneConfig : IEntityTypeConfiguration<ProjectMilestone>
{
    private const int TitleMaxLength = 300;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProjectMilestone> builder)
    {
        builder.ToTable("project_milestones");
        builder.ConfigureAuditable();

        builder.Property(m => m.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(m => m.DueDate).HasColumnName("due_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(m => m.IsCompleted).HasColumnName("is_completed").IsRequired();
        builder.Property(m => m.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ProjectId, m.SortOrder }).HasDatabaseName("ix_project_milestones_project_sort");
    }
}
