using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahSection"/> (DATA-MODEL.md section 3.8 <c>shorfah_sections</c>).</summary>
public sealed class ShorfahSectionConfig : IEntityTypeConfiguration<ShorfahSection>
{
    private const int TitleMaxLength = 300;
    private const int SectionTypeMaxLength = 30;
    private const int OwnerRoleMaxLength = 100;
    private const int WorkflowStatusMaxLength = 30;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahSection> builder)
    {
        builder.ToTable("shorfah_sections");
        builder.ConfigureAuditable();

        builder.Property(s => s.IssueId).HasColumnName("issue_id").IsRequired();
        builder.Property(s => s.ParentSectionId).HasColumnName("parent_section_id");
        builder.Property(s => s.SectionType).HasColumnName("section_type").HasMaxLength(SectionTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(s => s.TitleAr).HasColumnName("title_ar").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(s => s.DescriptionAr).HasColumnName("description_ar").HasColumnType("nvarchar(max)");
        builder.Property(s => s.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(s => s.OwnerUserId).HasColumnName("owner_user_id");
        builder.Property(s => s.OwnerRole).HasColumnName("owner_role").HasMaxLength(OwnerRoleMaxLength);
        builder.Property(s => s.IncludeInPdf).HasColumnName("include_in_pdf").IsRequired();
        builder.Property(s => s.AutoGenerate).HasColumnName("auto_generate");
        builder.Property(s => s.GenerationPrompt).HasColumnName("generation_prompt").HasColumnType("nvarchar(max)");
        builder.Property(s => s.WorkflowStatus).HasColumnName("workflow_status").HasMaxLength(WorkflowStatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(s => s.ContentMd).HasColumnName("content_md").HasColumnType("nvarchar(max)");
        builder.Property(s => s.ContentHtml).HasColumnName("content_html").HasColumnType("nvarchar(max)");
        builder.Property(s => s.ContributedByUserId).HasColumnName("contributed_by_user_id");
        builder.Property(s => s.ContributedAt).HasColumnName("contributed_at").HasColumnType("datetimeoffset(3)");
        builder.Property(s => s.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(s => s.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetimeoffset(3)");
        builder.Property(s => s.ReviewNotes).HasColumnName("review_notes").HasColumnType("nvarchar(max)");
        builder.Property(s => s.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(s => s.ApprovedAt).HasColumnName("approved_at").HasColumnType("datetimeoffset(3)");
        builder.Property(s => s.RejectionReason).HasColumnName("rejection_reason").HasColumnType("nvarchar(max)");
        builder.Property(s => s.SlaDays).HasColumnName("sla_days");
        builder.Property(s => s.SlaStartsAt).HasColumnName("sla_starts_at").HasColumnType("datetimeoffset(3)");
        builder.Property(s => s.SlaDeadline).HasColumnName("sla_deadline").HasColumnType("datetimeoffset(3)");

        builder.HasIndex(s => s.IssueId).HasDatabaseName("ix_shorfah_sections_issue_id");
        builder.HasIndex(s => s.WorkflowStatus).HasDatabaseName("ix_shorfah_sections_workflow_status");
        builder.HasIndex(s => s.SlaDeadline).HasDatabaseName("ix_shorfah_sections_sla_deadline");
        builder.HasIndex(s => s.ParentSectionId).HasDatabaseName("ix_shorfah_sections_parent_section_id");
        builder.HasIndex(s => s.OwnerUserId).HasDatabaseName("ix_shorfah_sections_owner_user_id");

        // Cascade: the issue is the section's mandatory parent container -- deleting an issue
        // should remove all its sections (DATA-MODEL.md section 4 flags issue_id as the single
        // highest-priority unenforced implied FK gap in the schema; enforced here).
        builder.HasOne(s => s.Issue).WithMany(i => i.Sections)
            .HasForeignKey(s => s.IssueId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: self-referential parent/child. SQL Server disallows a cascading self-FK
        // (would need multiple cascade paths analysis); Restrict also better reflects that a
        // parent section with children shouldn't disappear silently.
        builder.HasOne(s => s.ParentSection).WithMany(s => s.ChildSections)
            .HasForeignKey(s => s.ParentSectionId).OnDelete(DeleteBehavior.Restrict);

        // Restrict on all four user references below: these are workflow-history pointers
        // (owner/contributor/reviewer/approver). Cascading user deletion into section history
        // would silently destroy the audit trail; Restrict forces an explicit reassignment
        // policy decision at the application layer instead.
        builder.HasOne(s => s.OwnerUser).WithMany()
            .HasForeignKey(s => s.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ContributedByUser).WithMany()
            .HasForeignKey(s => s.ContributedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ReviewedByUser).WithMany()
            .HasForeignKey(s => s.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.ApprovedByUser).WithMany()
            .HasForeignKey(s => s.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
