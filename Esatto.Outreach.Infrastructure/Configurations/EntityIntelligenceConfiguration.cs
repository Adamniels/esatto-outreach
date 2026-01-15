using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class EntityIntelligenceConfiguration : IEntityTypeConfiguration<EntityIntelligence>
{
    public void Configure(EntityTypeBuilder<EntityIntelligence> b)
    {
        b.ToTable("entity_intelligence");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        // JSON properties
        b.Property(x => x.CompanyHooksJson);
        b.Property(x => x.PersonalHooksJson);
        b.Property(x => x.SourcesJson);
        
        // Text properties
        b.Property(x => x.SummarizedContext);

        b.Property(x => x.ResearchedAt)
            .IsRequired();

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.UpdatedUtc);

        // Foreign Key to Prospect (required, One-to-One)
        b.Property(x => x.ProspectId)
            .IsRequired();

        b.HasOne(x => x.Prospect)
            .WithOne(p => p.EntityIntelligence)
            .HasForeignKey<EntityIntelligence>(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade); // If Prospect is deleted, delete data

        // Index
        b.HasIndex(x => x.ResearchedAt);
    }
}
