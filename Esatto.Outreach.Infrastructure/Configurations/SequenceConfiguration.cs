using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class SequenceConfiguration : IEntityTypeConfiguration<Sequence>
{
    public void Configure(EntityTypeBuilder<Sequence> builder)
    {
        builder.ToTable("Sequences");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Mode)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Settings are an owned entity that map to columns in the Sequences table
        builder.OwnsOne(s => s.Settings, settingsBuilder =>
        {
            settingsBuilder.Property(ss => ss.EnrichCompany).HasColumnName("Setting_EnrichCompany");
            settingsBuilder.Property(ss => ss.EnrichContact).HasColumnName("Setting_EnrichContact");
            settingsBuilder.Property(ss => ss.ResearchSimilarities).HasColumnName("Setting_ResearchSimilarities");
            settingsBuilder.Property(ss => ss.MaxActiveProspectsPerDay).HasColumnName("Setting_MaxActiveProspectsPerDay");
        });

        // Foreign Key to the User
        builder.HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.SetNull); // Keep sequence even if User is deleted, or adjust based on domain needs
    }
}
