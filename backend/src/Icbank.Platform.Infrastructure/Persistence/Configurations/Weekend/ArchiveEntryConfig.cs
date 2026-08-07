using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Weekend;

/// <summary>EF Core mapping for <see cref="ArchiveEntry"/> (DATA-MODEL.md section 3.9 <c>archive_entries</c>).</summary>
public sealed class ArchiveEntryConfig : IEntityTypeConfiguration<ArchiveEntry>
{
    private const int TitleMaxLength = 300;
    private const int OccasionMaxLength = 200;
    private const int ToneMaxLength = 100;
    private const int SourceFileMaxLength = 260;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ArchiveEntry> builder)
    {
        builder.ToTable("archive_entries");
        builder.ConfigureAuditable();

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(e => e.BodyText).HasColumnName("body_text").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.Date).HasColumnName("date").HasColumnType("datetimeoffset(3)");
        builder.Property(e => e.Occasion).HasColumnName("occasion").HasMaxLength(OccasionMaxLength);
        builder.Property(e => e.Tone).HasColumnName("tone").HasMaxLength(ToneMaxLength);
        builder.Property(e => e.SourceFile).HasColumnName("source_file").HasMaxLength(SourceFileMaxLength);

        // Deviation: DATA-MODEL.md section 3.9 flags this vector as a brute-force JSON float
        // array with no SQL Server equivalent to pgvector. Kept as a JSON-backed nullable list
        // for this port -- no vector-store migration performed. See DOMAIN-PORT-NOTES.md.
        builder.Property(e => e.Embedding)
            .HasColumnName("embedding_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(NullableJsonListValueConverter.Create<float>())
            .Metadata.SetValueComparer(NullableJsonListValueConverter.CreateComparer<float>());
    }
}
