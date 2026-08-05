using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahIssue"/> (DATA-MODEL.md section 3.8 <c>shorfah_issues</c>).</summary>
public sealed class ShorfahIssueConfig : IEntityTypeConfiguration<ShorfahIssue>
{
    private const int TitleMaxLength = 300;
    private const int SubtitleMaxLength = 300;
    private const int UrlMaxLength = 500;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahIssue> builder)
    {
        builder.ToTable("shorfah_issues");
        builder.ConfigureAuditable();

        builder.Property(i => i.IssueNo).HasColumnName("issue_no").IsRequired();
        builder.Property(i => i.TitleAr).HasColumnName("title_ar").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(i => i.SubtitleAr).HasColumnName("subtitle_ar").HasMaxLength(SubtitleMaxLength);
        builder.Property(i => i.Month).HasColumnName("month").IsRequired();
        builder.Property(i => i.Year).HasColumnName("year").IsRequired();
        builder.Property(i => i.CoverImageUrl).HasColumnName("cover_image_url").HasMaxLength(UrlMaxLength);
        builder.Property(i => i.EditorLetter).HasColumnName("editor_letter").HasColumnType("nvarchar(max)");
        builder.Property(i => i.ContributionsOpenAt).HasColumnName("contributions_open_at").HasColumnType("datetimeoffset(3)");
        builder.Property(i => i.ContributionsCloseAt).HasColumnName("contributions_close_at").HasColumnType("datetimeoffset(3)");
        builder.Property(i => i.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(i => i.PublishedPdfUrl).HasColumnName("published_pdf_url").HasMaxLength(UrlMaxLength);
        builder.Property(i => i.PublishedAt).HasColumnName("published_at").HasColumnType("datetimeoffset(3)");
        builder.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.HasIndex(i => i.IssueNo).IsUnique().HasDatabaseName("ux_shorfah_issues_issue_no");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_shorfah_issues_status");
        builder.HasIndex(i => i.CreatedByUserId).HasDatabaseName("ix_shorfah_issues_created_by_user_id");

        // Restrict: DATA-MODEL.md section 4 flags created_by as an unenforced implied FK; now
        // enforced. Restrict preserves the issue if the creating user is later deleted.
        builder.HasOne(i => i.CreatedByUser).WithMany()
            .HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
