using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class ContactPersonConfiguration : IEntityTypeConfiguration<ContactPerson>
{
    public void Configure(EntityTypeBuilder<ContactPerson> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Title)
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .HasMaxLength(200);
            
        builder.Property(x => x.LinkedInUrl)
             .HasMaxLength(500);

        // One-to-Many: Prospect has many ContactPersons
        builder.HasOne(x => x.Prospect)
            .WithMany(p => p.ContactPersons)
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
