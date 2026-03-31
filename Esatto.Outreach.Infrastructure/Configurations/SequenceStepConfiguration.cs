using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class SequenceStepConfiguration : IEntityTypeConfiguration<SequenceStep>
{
    public void Configure(EntityTypeBuilder<SequenceStep> builder)
    {
        builder.ToTable("SequenceSteps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.TimeOfDayToRun)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.GenerationType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // One-to-Many mapping from Sequence -> SequenceSteps
        builder.HasOne(s => s.Sequence)
            .WithMany(seq => seq.SequenceSteps)
            .HasForeignKey(s => s.SequenceId)
            .OnDelete(DeleteBehavior.Cascade); // If sequence deletes, delete steps
    }
}
