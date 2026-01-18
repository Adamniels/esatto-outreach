using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure;

// klassen som vi ärver irån är dbcontext + att den lägger till Identity tabeller
public class OutreachDbContext : IdentityDbContext<ApplicationUser>
{
    // base(options) gör så det skickas vidare och jag kan registrera i dependency injection istället
    public OutreachDbContext(DbContextOptions<OutreachDbContext> options) : base(options) { }

    // Betyder bara: "jag har en tabell för prospect Entities"
    public DbSet<Prospect> Prospects => Set<Prospect>();

    public DbSet<HardCompanyData> HardCompanyData => Set<HardCompanyData>();

    public DbSet<EntityIntelligence> EntityIntelligences => Set<EntityIntelligence>();
    
    public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();

    public DbSet<GenerateEmailPrompt> GenerateEmailPrompts => Set<GenerateEmailPrompt>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // VIKTIGT! Identity behöver detta

        // gör så jag använder det konifiguration som jag gjort i: ProspectConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutreachDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Ignorera pending model changes warning - value comparers påverkar inte databasschemat
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
}
