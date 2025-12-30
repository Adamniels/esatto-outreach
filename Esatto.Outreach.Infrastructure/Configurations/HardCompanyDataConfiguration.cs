using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class HardCompanyDataConfiguration : IEntityTypeConfiguration<HardCompanyData>
{
    public void Configure(EntityTypeBuilder<HardCompanyData> b)
    {
        b.ToTable("hard_company_data");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        b.Property(x => x.CompanyOverview);

        // JSON-fält lagras som text (SQLite) eller JSONB (PostgreSQL via migration)
        b.Property(x => x.ServicesJson);
        
        b.Property(x => x.CasesJson);
        
        b.Property(x => x.IndustriesJson);
        
        b.Property(x => x.KeyFactsJson);
        
        b.Property(x => x.SourcesJson);

        b.Property(x => x.ResearchedAt)
            .IsRequired();

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.UpdatedUtc);

        // Index för att snabbt hitta recent research
        b.HasIndex(x => x.ResearchedAt);
    }
}
