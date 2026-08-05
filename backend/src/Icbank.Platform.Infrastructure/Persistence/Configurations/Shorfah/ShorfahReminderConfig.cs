using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahReminder"/> (DATA-MODEL.md section 3.8 <c>shorfah_reminders</c>).</summary>
public sealed class ShorfahReminderConfig : IEntityTypeConfiguration<ShorfahReminder>
{
    private const int ChannelMaxLength = 10;
    private const int ReminderTypeMaxLength = 20;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahReminder> builder)
    {
        builder.ToTable("shorfah_reminders");
        builder.ConfigureAuditable();

        builder.Property(r => r.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(r => r.AssignmentId).HasColumnName("assignment_id");
        builder.Property(r => r.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(r => r.Channel).HasColumnName("channel").HasMaxLength(ChannelMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.ReminderType).HasColumnName("reminder_type").HasMaxLength(ReminderTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.SentAt).HasColumnName("sent_at").HasColumnType("datetimeoffset(3)");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(StatusMaxLength);
        builder.Property(r => r.Message).HasColumnName("message").HasColumnType("nvarchar(max)");

        builder.HasIndex(r => r.SectionId).HasDatabaseName("ix_shorfah_reminders_section_id");
        builder.HasIndex(r => r.AssignmentId).HasDatabaseName("ix_shorfah_reminders_assignment_id");
        builder.HasIndex(r => r.RecipientUserId).HasDatabaseName("ix_shorfah_reminders_recipient_user_id");

        // Cascade: a reminder log row has no meaning once its section is gone.
        builder.HasOne(r => r.Section).WithMany(s => s.Reminders)
            .HasForeignKey(r => r.SectionId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: preserves the reminder log if the assignment/recipient is later removed --
        // this is a delivery audit trail, not a live business rule.
        builder.HasOne(r => r.Assignment).WithMany(a => a.Reminders)
            .HasForeignKey(r => r.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RecipientUser).WithMany()
            .HasForeignKey(r => r.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
