using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class SoftCompanyDataConfiguration : IEntityTypeConfiguration<SoftCompanyData>
{
    public void Configure(EntityTypeBuilder<SoftCompanyData> b)
    {
        b.ToTable("soft_company_data");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        // JSON-fält lagras som text (SQLite) eller JSONB (PostgreSQL via migration)
        b.Property(x => x.HooksJson);
        
        b.Property(x => x.RecentEventsJson);
        
        b.Property(x => x.NewsItemsJson);
        
        b.Property(x => x.SocialActivityJson);
        
        b.Property(x => x.SourcesJson);

        b.Property(x => x.ResearchedAt)
            .IsRequired();

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.UpdatedUtc);

        // Foreign Key till Prospect (required, One-to-One)
        b.Property(x => x.ProspectId)
            .IsRequired();

        b.HasOne(x => x.Prospect)
            .WithOne(p => p.SoftCompanyData)
            .HasForeignKey<SoftCompanyData>(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade); // Om Prospect raderas, radera soft data också

        // Index för att snabbt hitta recent research
        b.HasIndex(x => x.ResearchedAt);
    }
}
