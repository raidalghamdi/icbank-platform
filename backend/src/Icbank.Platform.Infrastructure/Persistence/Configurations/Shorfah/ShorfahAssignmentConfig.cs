using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahAssignment"/> (DATA-MODEL.md section 3.8 <c>shorfah_assignments</c>).</summary>
public sealed class ShorfahAssignmentConfig : IEntityTypeConfiguration<ShorfahAssignment>
{
    private const int RoleMaxLength = 50;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahAssignment> builder)
    {
        builder.ToTable("shorfah_assignments");
        builder.ConfigureAuditable();

        builder.Property(a => a.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.Role).HasColumnName("role").HasMaxLength(RoleMaxLength);

        builder.HasIndex(a => a.SectionId).HasDatabaseName("ix_shorfah_assignments_section_id");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_shorfah_assignments_user_id");
        builder.HasIndex(a => new { a.SectionId, a.UserId }).IsUnique().HasDatabaseName("ux_shorfah_assignments_section_user");

        // Cascade: an assignment has no meaning once its section is gone.
        builder.HasOne(a => a.Section).WithMany(s => s.Assignments)
            .HasForeignKey(a => a.SectionId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: preserves assignment history if the assigned user is later deleted.
        builder.HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
