using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class ProspectConfiguration : IEntityTypeConfiguration<Prospect>
{
    public void Configure(EntityTypeBuilder<Prospect> b)
    {
        b.ToTable("prospects");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        b.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Domain)
            .HasMaxLength(200);

        b.Property(x => x.ContactName)
            .HasMaxLength(200);

        b.Property(x => x.ContactEmail)
            .HasMaxLength(320); // RFC-ish

        b.Property(x => x.LinkedinUrl)
            .HasMaxLength(500);

        b.Property(x => x.Notes);

        // Enum → string i databasen (läsbart och stabilt)
        b.Property(x => x.Status)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ProspectStatus>(v))
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.UpdatedUtc);

        b.Property(x => x.MailTitle)
         .HasMaxLength(200);

        b.Property(x => x.MailBodyPlain)
        .HasColumnType("text"); // långtext

        b.Property(x => x.MailBodyHTML)
        .HasColumnType("text"); // långtext

        // Foreign Key till HardCompanyData (nullable, One-to-One)
        b.Property(x => x.HardCompanyDataId);

        b.HasOne(x => x.HardCompanyData)
            .WithMany()
            .HasForeignKey(x => x.HardCompanyDataId)
            .OnDelete(DeleteBehavior.SetNull); // Om HardCompanyData raderas, sätt FK till null

        // Foreign Key till SoftCompanyData (nullable, One-to-One)
        b.Property(x => x.SoftCompanyDataId);

        b.HasOne(x => x.SoftCompanyData)
            .WithOne(s => s.Prospect)
            .HasForeignKey<SoftCompanyData>(s => s.ProspectId)
            .OnDelete(DeleteBehavior.Cascade); // Om Prospect raderas, radera soft data också

        b.HasIndex(x => x.CompanyName);
        b.HasIndex(x => x.Domain);
        b.HasIndex(x => new { x.Status, x.CreatedUtc });
        b.HasIndex(x => x.MailTitle);
        b.HasIndex(x => x.HardCompanyDataId);
        b.HasIndex(x => x.SoftCompanyDataId);
    }
}
