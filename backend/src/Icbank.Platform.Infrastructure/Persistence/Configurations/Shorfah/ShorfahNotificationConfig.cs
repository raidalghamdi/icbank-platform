using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahNotification"/> (DATA-MODEL.md section 3.8 <c>shorfah_notifications</c>).</summary>
public sealed class ShorfahNotificationConfig : IEntityTypeConfiguration<ShorfahNotification>
{
    private const int TypeMaxLength = 50;
    private const int TitleMaxLength = 300;
    private const int UrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahNotification> builder)
    {
        builder.ToTable("shorfah_notifications");
        builder.ConfigureAuditable();

        builder.Property(n => n.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(n => n.IssueId).HasColumnName("issue_id");
        builder.Property(n => n.SectionId).HasColumnName("section_id");
        builder.Property(n => n.Type).HasColumnName("type").HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(n => n.Body).HasColumnName("body").HasColumnType("nvarchar(max)");
        builder.Property(n => n.Url).HasColumnName("url").HasMaxLength(UrlMaxLength);
        builder.Property(n => n.IsRead).HasColumnName("is_read");

        builder.HasIndex(n => n.UserId).HasDatabaseName("ix_shorfah_notifications_user_id");
        builder.HasIndex(n => n.IssueId).HasDatabaseName("ix_shorfah_notifications_issue_id");
        builder.HasIndex(n => n.SectionId).HasDatabaseName("ix_shorfah_notifications_section_id");
        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("ix_shorfah_notifications_user_unread");

        // Cascade: a notification belongs entirely to its recipient's inbox.
        builder.HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        // Restrict on Issue/Section: prevents a large-scale notification wipeout when an issue
        // or section is deleted -- inbox history should be reviewable independently of whether
        // the source item still exists.
        builder.HasOne(n => n.Issue).WithMany(i => i.Notifications)
            .HasForeignKey(n => n.IssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(n => n.Section).WithMany(s => s.Notifications)
            .HasForeignKey(n => n.SectionId).OnDelete(DeleteBehavior.Restrict);
    }
}
