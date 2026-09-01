using Icbank.Platform.Domain.Projects;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Projects;

/// <summary>EF Core mapping for <see cref="PortfolioProject"/>.</summary>
public sealed class PortfolioProjectConfig : IEntityTypeConfiguration<PortfolioProject>
{
    private const int CodeMaxLength = 40;
    private const int NameMaxLength = 300;
    private const int DescriptionMaxLength = 600;
    private const int OwnerMaxLength = 150;
    private const int DepartmentMaxLength = 150;
    private const int LatestUpdateMaxLength = 600;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PortfolioProject> builder)
    {
        builder.ToTable("portfolio_projects");
        builder.ConfigureAuditable();

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(CodeMaxLength).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(DescriptionMaxLength).IsRequired();
        builder.Property(p => p.Category).HasColumnName("category").HasConversion<int>().IsRequired();
        builder.Property(p => p.Stage).HasColumnName("stage").HasConversion<int>().IsRequired();
        builder.Property(p => p.Owner).HasColumnName("owner").HasMaxLength(OwnerMaxLength).IsRequired();
        builder.Property(p => p.Department).HasColumnName("department").HasMaxLength(DepartmentMaxLength).IsRequired();
        builder.Property(p => p.ProgressPercent).HasColumnName("progress_percent").IsRequired();
        builder.Property(p => p.TeamSize).HasColumnName("team_size").IsRequired();
        builder.Property(p => p.StartDate).HasColumnName("start_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(p => p.DueDate).HasColumnName("due_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(p => p.LatestUpdate).HasColumnName("latest_update").HasMaxLength(LatestUpdateMaxLength).IsRequired();
        builder.Property(p => p.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasIndex(p => p.Code).HasDatabaseName("ix_portfolio_projects_code");
        builder.HasIndex(p => new { p.Category, p.SortOrder }).HasDatabaseName("ix_portfolio_projects_category_sort");
    }
}
