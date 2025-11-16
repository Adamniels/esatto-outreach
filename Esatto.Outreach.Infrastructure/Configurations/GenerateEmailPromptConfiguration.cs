using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public sealed class GenerateEmailPromptConfiguration : IEntityTypeConfiguration<GenerateEmailPrompt>
{
    public void Configure(EntityTypeBuilder<GenerateEmailPrompt> builder)
    {
        builder.ToTable("generate_email_prompts");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.Instructions)
            .IsRequired()
            .HasColumnType("text"); // Långtext för prompten
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedUtc)
            .IsRequired();
        
        builder.Property(x => x.UpdatedUtc)
            .IsRequired();
        
        // Index för snabb lookup av aktiv prompt
        builder.HasIndex(x => x.IsActive);
    }
}
