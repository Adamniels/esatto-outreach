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

        // ========== CRM IDENTITY ==========
        b.Property(x => x.CrmSource)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.ExternalCrmId)
            .HasMaxLength(100);

        // ========== CORE DATA ==========
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.About)
            .HasColumnType("text");

        b.Property(x => x.CrmCreatedAt);

        b.Property(x => x.CrmUpdatedAt);

        b.Property(x => x.LastContactedAt);

        b.Property(x => x.PictureURL)
            .HasMaxLength(500);

        // Nested collections as JSONB (generic types, CRM-agnostic)
        b.Property(x => x.Websites)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Website>>(v, (JsonSerializerOptions?)null) ?? new List<Website>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Website>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Tag>>(v, (JsonSerializerOptions?)null) ?? new List<Tag>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Tag>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        b.Property(x => x.CustomFields)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<CustomField>>(v, (JsonSerializerOptions?)null) ?? new List<CustomField>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<CustomField>>(
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
        // External CRM ID (unique per CRM provider)
        b.HasIndex(x => new { x.CrmSource, x.ExternalCrmId })
            .IsUnique()
            .HasFilter("\"ExternalCrmId\" IS NOT NULL");

        // Pending prospects
        b.HasIndex(x => x.IsPending);

        // Composite for pending + created (sorting)
        b.HasIndex(x => new { x.IsPending, x.CreatedUtc });

        // Owner queries
        b.HasIndex(x => x.OwnerId);

        // Owner + status
        b.HasIndex(x => new { x.OwnerId, x.Status });

        // Name (for searches)
        b.HasIndex(x => x.Name);

        // Status + created (for filtering)
        b.HasIndex(x => new { x.Status, x.CreatedUtc });

        // Relations
        b.HasIndex(x => x.EntityIntelligenceId);
    }
}
