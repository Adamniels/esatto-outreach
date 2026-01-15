using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class ProspectConfiguration : IEntityTypeConfiguration<Prospect>
{
    public void Configure(EntityTypeBuilder<Prospect> b)
    {
        b.ToTable("prospects");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .ValueGeneratedNever();

        // ========== CAPSULE CRM DATA ==========
        b.Property(x => x.CapsuleId);

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.About)
            .HasColumnType("text");

        b.Property(x => x.CapsuleCreatedAt);

        b.Property(x => x.CapsuleUpdatedAt);

        b.Property(x => x.LastContactedAt);

        b.Property(x => x.PictureURL)
            .HasMaxLength(500);

        // Nested collections som JSONB
        b.Property(x => x.Websites)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsuleWebsite>>(v, (JsonSerializerOptions?)null) ?? new List<CapsuleWebsite>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsuleWebsite>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.EmailAddresses)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsuleEmailAddress>>(v, (JsonSerializerOptions?)null) ?? new List<CapsuleEmailAddress>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsuleEmailAddress>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.PhoneNumbers)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsulePhoneNumber>>(v, (JsonSerializerOptions?)null) ?? new List<CapsulePhoneNumber>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsulePhoneNumber>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.Addresses)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsuleAddress>>(v, (JsonSerializerOptions?)null) ?? new List<CapsuleAddress>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsuleAddress>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.Tags)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsuleTag>>(v, (JsonSerializerOptions?)null) ?? new List<CapsuleTag>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsuleTag>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.CustomFields)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CapsuleCustomField>>(v, (JsonSerializerOptions?)null) ?? new List<CapsuleCustomField>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CapsuleCustomField>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        // ========== ESATTO WORKFLOW ==========
        b.Property(x => x.IsPending)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.Notes)
            .HasColumnType("text");

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

        b.Property(x => x.MailBodyHTML)
            .HasColumnType("text");

        b.Property(x => x.LastOpenAIResponseId)
            .HasMaxLength(200);

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.UpdatedUtc);

        // ========== RELATIONER ==========
        // Foreign Key till HardCompanyData (nullable, One-to-One)
        b.Property(x => x.HardCompanyDataId);

        b.HasOne(x => x.HardCompanyData)
            .WithMany()
            .HasForeignKey(x => x.HardCompanyDataId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign Key till EntityIntelligence (nullable, One-to-One)
        b.Property(x => x.EntityIntelligenceId);

        b.HasOne(x => x.EntityIntelligence)
            .WithOne(s => s.Prospect)
            .HasForeignKey<EntityIntelligence>(s => s.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========== OWNERSHIP ==========
        // OwnerId är nullable för pending Capsule prospects
        b.Property(x => x.OwnerId)
            .HasMaxLength(450);

        b.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== INDEX ==========
        // CapsuleId (unique för Capsule prospects)
        b.HasIndex(x => x.CapsuleId)
            .IsUnique()
            .HasFilter("\"CapsuleId\" IS NOT NULL");  // Partial unique index

        // Pending prospects (viktigt för snabb hämtning)
        b.HasIndex(x => x.IsPending);

        // Composite för pending + created (sortering)
        b.HasIndex(x => new { x.IsPending, x.CreatedUtc });

        // Owner queries
        b.HasIndex(x => x.OwnerId);

        // Owner + status
        b.HasIndex(x => new { x.OwnerId, x.Status });

        // Name (för sökningar)
        b.HasIndex(x => x.Name);

        // Status + created (för filtrering)
        b.HasIndex(x => new { x.Status, x.CreatedUtc });

        // Relationer
        b.HasIndex(x => x.HardCompanyDataId);
        b.HasIndex(x => x.EntityIntelligenceId);
    }
}
