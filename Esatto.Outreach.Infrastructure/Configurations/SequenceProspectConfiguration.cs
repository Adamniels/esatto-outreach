using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class SequenceProspectConfiguration : IEntityTypeConfiguration<SequenceProspect>
{
    public void Configure(EntityTypeBuilder<SequenceProspect> builder)
    {
        builder.ToTable("SequenceProspects");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sp => sp.FailureReason)
            .HasMaxLength(2000);

        builder.Property(sp => sp.RowVersion)
            .IsRowVersion()
            .HasDefaultValueSql("gen_random_bytes(16)");

        // One-to-Many mapping from Sequence -> SequenceProspect
        builder.HasOne(sp => sp.Sequence)
            .WithMany(seq => seq.SequenceProspects)
            .HasForeignKey(sp => sp.SequenceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mapping to Prospect
        builder.HasOne(sp => sp.Prospect)
            .WithMany() // Assuming we don't need reverse navigation from Prospect to SequenceProspects yet
            .HasForeignKey(sp => sp.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mapping to ContactPerson
        builder.HasOne(sp => sp.Contact)
            .WithMany()
            .HasForeignKey(sp => sp.ContactPersonId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Indices required for Orchestrator background work
        builder.HasIndex(sp => new { sp.Status, sp.NextStepScheduledAt })
            .HasDatabaseName("IX_SequenceProspects_WorkerQueue");
    }
}
