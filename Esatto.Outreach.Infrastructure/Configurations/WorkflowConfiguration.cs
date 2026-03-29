using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Esatto.Outreach.Infrastructure.Configurations;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> b)
    {
        b.ToTable("WorkflowTemplates");

        b.HasKey(x => x.Id);
        
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Description)
            .HasMaxLength(1000);

        b.HasMany(x => x.Steps)
            .WithOne()
            .HasForeignKey(x => x.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowTemplateStepConfiguration : IEntityTypeConfiguration<WorkflowTemplateStep>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplateStep> b)
    {
        b.ToTable("WorkflowTemplateSteps");

        b.HasKey(x => x.Id);

        b.Property(x => x.StepType)
            .HasConversion<string>();
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> b)
    {
        b.ToTable("WorkflowInstances");

        b.HasKey(x => x.Id);

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // One-to-One or Many-to-One with Prospect? 
        // A prospect could theoretically have multiple workflows over time (history),
        // but typically one active. For now, let's say One Prospect has Many WorkflowInstances
        // but we might enforce logic to have only one ACTIVE.
        
        // Relationship
        b.HasOne(x => x.Prospect)
            .WithMany() // or WithOne(p => p.Workflow) if we added it to Prospect
            .HasForeignKey(x => x.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Steps)
            .WithOne(s => s.WorkflowInstance)
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ProspectId);
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> b)
    {
        b.ToTable("WorkflowSteps");

        b.HasKey(x => x.Id);

        b.Property(x => x.Type)
            .HasConversion<string>();

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        b.Property(x => x.RowVersion)
            .IsRowVersion()
            .ValueGeneratedOnAddOrUpdate();

        b.Property(x => x.EmailSubject)
            .HasMaxLength(500);
            
        b.Property(x => x.BodyContent)
            .HasColumnType("text");
            
        // Indices for the Polish Worker
        // It needs to find: Pending steps where RunAt <= Now
        b.HasIndex(x => new { x.Status, x.RunAt });
        
        b.HasIndex(x => x.WorkflowInstanceId);
    }
}
