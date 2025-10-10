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

        b.HasIndex(x => x.CompanyName);
        b.HasIndex(x => x.Domain);
        b.HasIndex(x => new { x.Status, x.CreatedUtc });
        b.HasIndex(x => x.MailTitle);
    }
}
