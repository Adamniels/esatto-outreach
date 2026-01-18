using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class EntityIntelligenceConfiguration : IEntityTypeConfiguration<EntityIntelligence>
{
    public void Configure(EntityTypeBuilder<EntityIntelligence> b)
    {
        b.ToTable("entity_intelligence");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        // REMOVED Legacy JSON properties
        // b.Property(x => x.CompanyHooksJson);
        // b.Property(x => x.PersonalHooksJson);
        // b.Property(x => x.SourcesJson);
        
        // Text properties
        b.Property(x => x.SummarizedContext);

        // Enrichment
        b.Property(x => x.EnrichmentVersion)
            .HasMaxLength(50);

        // Structured Enrichment Data (EF Core JSON mapping via ValueConverter)
        // Using ValueConverter solves deep nesting NRE issues in EF Core 9.
        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
            WriteIndented = false 
        };

        b.Property(x => x.EnrichedData)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<CompanyEnrichmentResult>(v, jsonOptions) ?? new CompanyEnrichmentResult(),
                new ValueComparer<CompanyEnrichmentResult>(
                    (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
                    c => c == null ? 0 : JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
                    c => JsonSerializer.Deserialize<CompanyEnrichmentResult>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions)!
                )
            );

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
