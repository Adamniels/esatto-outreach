using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class ProjectCaseConfiguration : IEntityTypeConfiguration<ProjectCase>
{
    public void Configure(EntityTypeBuilder<ProjectCase> builder)
    {
        builder.ToTable("project_cases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientName).IsRequired();
        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.Property(x => x.UpdatedUtc).IsRequired(false);

        builder.HasOne(x => x.Company)
               .WithMany(x => x.ProjectCases)
               .HasForeignKey(x => x.CompanyId)
               .IsRequired();
    }
}
