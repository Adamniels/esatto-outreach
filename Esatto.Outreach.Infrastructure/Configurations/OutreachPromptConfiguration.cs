using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public sealed class OutreachPromptConfiguration : IEntityTypeConfiguration<OutreachPrompt>
{
    public void Configure(EntityTypeBuilder<OutreachPrompt> builder)
    {
        builder.ToTable("outreach_prompts");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.Instructions)
            .IsRequired()
            .HasColumnType("text"); // Långtext för prompten
        
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(x => x.CreatedUtc)
            .IsRequired();
        
        builder.Property(x => x.UpdatedUtc)
            .IsRequired();
        
        // Foreign key to AspNetUsers
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Index för snabb lookup av aktiv prompt per user och typ
        builder.HasIndex(x => new { x.UserId, x.Type, x.IsActive });
        builder.HasIndex(x => x.UserId);
    }
}
