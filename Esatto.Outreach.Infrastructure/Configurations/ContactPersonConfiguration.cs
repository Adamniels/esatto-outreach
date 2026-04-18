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

        // JSONB columns for lists
        builder.Property(x => x.PersonalHooks)
            .HasColumnName("PersonalHooksJson")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        builder.Property(x => x.PersonalNews)
            .HasColumnName("PersonalNewsJson")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        // IsActive property for marking the active contact for email generation
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        // Partial unique index: ensures only one active contact per prospect
        // Allows multiple IsActive=false, but only one IsActive=true per ProspectId
        builder.HasIndex(x => new { x.ProspectId, x.IsActive })
            .IsUnique()
            .HasFilter("\"IsActive\" = true")
            .HasDatabaseName("IX_ContactPersons_ProspectId_IsActive");
    }
}
