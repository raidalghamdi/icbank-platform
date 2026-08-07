using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.AiYear;

/// <summary>
/// EF Core mapping for <see cref="AiYearActivationChannel"/>. This table does not exist in the
/// source Postgres schema -- it normalizes the source's native <c>channels text[]</c> array
/// column (AMBIGUOUS-2 in DATA-MODEL.md) into a proper child table. See DOMAIN-PORT-NOTES.md.
/// </summary>
public sealed class AiYearActivationChannelConfig : IEntityTypeConfiguration<AiYearActivationChannel>
{
    private const int ChannelMaxLength = 50;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiYearActivationChannel> builder)
    {
        builder.ToTable("ai_year_activation_channels");
        builder.ConfigureAuditable();

        builder.Property(c => c.ActivationId).HasColumnName("activation_id").IsRequired();
        builder.Property(c => c.Channel).HasColumnName("channel").HasMaxLength(ChannelMaxLength).IsRequired();

        builder.HasIndex(c => c.ActivationId).HasDatabaseName("ix_ai_year_activation_channels_activation_id");
        builder.HasIndex(c => new { c.ActivationId, c.Channel }).IsUnique().HasDatabaseName("ux_ai_year_activation_channels_activation_channel");

        // Cascade: a channel row has no meaning once its parent activation is gone -- mirrors
        // the cascading intent already used for ai_year_media/ai_year_metrics in the source.
        builder.HasOne(c => c.Activation).WithMany(a => a.Channels)
            .HasForeignKey(c => c.ActivationId).OnDelete(DeleteBehavior.Cascade);
    }
}
