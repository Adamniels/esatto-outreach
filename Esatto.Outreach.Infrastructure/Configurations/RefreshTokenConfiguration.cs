using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        
        builder.HasKey(rt => rt.Id);
        
        builder.Property(rt => rt.Id)
            .ValueGeneratedNever();
        
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.HasIndex(rt => rt.Token)
            .IsUnique();
        
        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasMaxLength(450); // Same as AspNetUsers.Id
        
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();
        
        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(rt => rt.CreatedUtc)
            .IsRequired();
        
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.ExpiresAt);
    }
}
