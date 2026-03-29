using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class CompanyInformationConfiguration : IEntityTypeConfiguration<CompanyInformation>
{
    public void Configure(EntityTypeBuilder<CompanyInformation> builder)
    {
        builder.ToTable("company_informations");

        builder.HasKey(x => x.Id);


        builder.Property(x => x.Overview).IsRequired();
        builder.Property(x => x.ValueProposition).IsRequired();

        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired(false);

        builder.HasOne(x => x.Company)
               .WithOne(x => x.CompanyInformation)
               .HasForeignKey<CompanyInformation>(x => x.CompanyId)
               .IsRequired();
    }
}
